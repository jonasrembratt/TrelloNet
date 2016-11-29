using System;
using System.Collections.Generic;
using System.Linq;
using Tetrapak.ToCommon;
using TrelloNet;
using TrelloNet.Extensions;
using TrelloNet.Internal;
using Action = TrelloNet.Action;

namespace Trello.net.api
{
    public class MyTrelloApi
    {
        private const string ApplicationKey = "6c4842f4580be317adfeaa07d3f7f1e8";
        private readonly ITrello _trello;
        private string[] _doingListsIds;
        private string[] _doneListsIds;
        private BoardAnalysisResult _boardAnalysis;

        private Board Board { get; set; }

        private IUserInterface UI { get; }

        public Uri AuthorizationUrl { get; }

        public bool IsAuthorized { get; private set; }

        public bool TryAuthenticate(string authorizationToken)
        {
            try
            {
                _trello.Authorize(authorizationToken);
                IsAuthorized = true;
                return true;
            }
            catch (Exception ex)
            {
                UI?.Error($"Authentication failed! {ex}");
                IsAuthorized = true;
                return false;
            }
        }

        public void SelectBoard(string boardId)
        {
            if (string.IsNullOrEmpty(boardId)) throw error(new ArgumentNullException(nameof(boardId)));
            if (boardId == Board?.Id)
                return;

            var board = _trello.Boards.WithId(boardId);
            if (board == null)
                throw new Exception($"Cannot find board with id {boardId}.");

            Board = board;
            UI?.WriteLine($"Board selected: {board.Name}");
        }

        public void SetDoingListsIds(params string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                _doingListsIds = null;
                return;
            }
            ensureBoardIsSelected();
            _doingListsIds = _trello.Lists.ForBoard(Board)
                .Where(l => ids.Any(s => s.Equals(l.Name, StringComparison.OrdinalIgnoreCase) || l.Id == s))
                .Select(l => l.Id).ToArray();
            UI?.WriteLine($"Sets 'doing' lists to: {HelpersAndExtensions.ToString(ids)}");
        }

        public void SetDoneListsIds(params string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                _doneListsIds = null;
                return;
            }
            ensureBoardIsSelected();
            _doneListsIds = _trello.Lists.ForBoard(Board)
                .Where(l => ids.Any(s => s.Equals(l.Name, StringComparison.OrdinalIgnoreCase) || l.Id == s))
                .Select(l => l.Id).ToArray();
            UI?.WriteLine($"Sets 'done' lists to: {HelpersAndExtensions.ToString(ids)}");
        }

        public MyTrelloApi(string authorizationToken = null, IUserInterface ui = null)
        {
            UI = ui;
            _trello = new TrelloNet.Trello(ApplicationKey);
            if (string.IsNullOrEmpty(authorizationToken) || !TryAuthenticate(authorizationToken))
            {
                AuthorizationUrl = _trello.GetAuthorizationUrl("MyTrelloApi", Scope.ReadOnly, Expiration.Never);
                IsAuthorized = false;
            }
        }

        public List<BugStatInPeriod> GetBugsPerMonth()
        {
            ensureBoardIsSelected();
            var result = new Dictionary<string, BugStatInPeriod>();
            foreach (var card in _trello.Cards.ForBoard(Board))
            {
                if (!card.IsBug())
                    continue;

                var dateCreated = card.DateCreated();
                var month = dateCreated.ToString("yy-MM");
                BugStatInPeriod bugStat;
                if (!result.TryGetValue(month, out bugStat))
                {
                    var start = new DateTime(dateCreated.Year, dateCreated.Month, 1);
                    var end = start.Subtract(TimeSpan.FromDays(1));
                    result.Add(month, bugStat = new BugStatInPeriod(start, end));
                }
                bugStat.Add();
            }
            return result.Values.ToList();
        }

        public LeadTimesResult GetLeadTimes(DateTime? start = null, DateTime? end = null, TimeGranularity granularity = TimeGranularity.Week)
        {
            try
            {
                AnalyzePeriod(start, end, granularity);
                var totalCount = 0;
                var accumulatedLeadTimes = TimeSpan.Zero;
                var result = new LeadTimesResult(granularity);
                foreach (var period in _boardAnalysis.Periods)
                {
                    var periodCount = 0;
                    var periodAccumulatedLeadTimes = TimeSpan.Zero;
                    foreach (var card in period.Done)
                    {
                        var leadTime = card.LeadTime;
                        if (card.LeadTime == TimeSpan.Zero)
                            continue;

                        accumulatedLeadTimes += leadTime;
                        periodAccumulatedLeadTimes += leadTime;
                        ++totalCount;
                        ++periodCount;
                    }
                    if (periodCount == 0)
                        continue;
                    var periodLeadTimes = TimeSpan.FromSeconds(periodAccumulatedLeadTimes.TotalSeconds / periodCount);
                    result.AddPeriod(new LeadTimesResultPeriod(period, periodLeadTimes));
                }
                var overallLeadTimes = TimeSpan.FromSeconds(accumulatedLeadTimes.TotalSeconds / totalCount);
                result.LeadTimes = overallLeadTimes;
                return result;
            }
            catch (Exception nisse)
            {
                throw;
            }
        }

        public BoardAnalysisResult AnalyzePeriod(DateTime? start = null, DateTime? end = null, TimeGranularity granularity = TimeGranularity.Week)
        {
            const string DataFolder = ".\\data";
            ensureBoardIsSelected();
            ensureProgressListsAreSpecified();
            var startDate = start.HasValue && start.Value != default(DateTime) ? start.Value : projectStartTime();
            var endDate = end.HasValue && end.Value != default(DateTime) ? end.Value : DateTime.Today.AddDays(1).Subtract(TimeSpan.FromSeconds(1));
            if (_boardAnalysis?.Equals(startDate, endDate, granularity) ?? false)
                return _boardAnalysis;

            var db = new ApiDatastore(DataFolder);
            BoardAnalysisResult boardAnalysis;
            var analysisExists = false;
            ISince since = new SinceDate(startDate);
            if (db.TryLoadBoardAnalysis(Board.Id, startDate, endDate, granularity, out boardAnalysis))
            {
                _boardAnalysis = boardAnalysis;
                since = new SinceDate(boardAnalysis.Timestamp);
                analysisExists = true;
            }
            UI?.Write("Reads cards from Trello ... ");
            var allCards = _trello.Cards.ForBoard(Board).ToList();
            UI?.WriteLine($"DONE (board '{Board.Name}' contains {allCards.Count} cards)");
            UI?.WriteLine("Analysing card actions ...");
            UI?.ShowBusy(maxValue: allCards.Count);
            try
            {
                Func<CreateCardAction, CardStatus> createStatus = action =>
                {
                    if (_doingListsIds.Any(id => id == action.Data.List.Id))
                        return CardStatus.Doing;
                    return _doneListsIds.Any(id => id == action.Data.List.Id) ? CardStatus.Done : CardStatus.Other;
                };
                Func<MoveCardAction, CardStatus> moveStatus = action =>
                {
                    if (_doingListsIds.Any(id => id == action.ListAfter.Id))
                        return CardStatus.Doing;
                    return _doneListsIds.Any(id => id == action.ListAfter.Id) ? CardStatus.Done : CardStatus.Other;
                };

                if (!analysisExists)
                    _boardAnalysis = new BoardAnalysisResult(Board.Id, startDate, endDate, granularity);

                // generate all periods ...
                PeriodCardsStatus lastPeriod;
                Dictionary<DateTime, PeriodCardsStatus> result;
                var firstPeriod = generatePeriods(startDate, ref endDate, granularity, analysisExists, out lastPeriod, out result);
                var progress = 0;
                ICardActionsProvider cardActionsProvider;
                if (!analysisExists || !tryQuickAnalyzePeriodThroughActions(out cardActionsProvider))
                    cardActionsProvider = new CardActionsReader(_trello, since, endDate);

                var analysisWasUpdated = false;
                foreach (var card in allCards)
                {
                    UI?.UpdateBusy(progress, $" | Card: '{card.Name}'");
                    var allActions = cardActionsProvider.GetCardActions(card);
                    if (allActions.Count == 0)
                    {
                        UI?.UpdateBusy(++progress);
                        continue;
                    }
                    var createActions = allActions.OfType<CreateCardAction>().Where(a => a.Date <= endDate).ToList();
                    var trelloCard = _boardAnalysis.Cards.FirstOrDefault(c => c.Id == card.Id);
                    PeriodCardsStatus period;
                    foreach (var createAction in createActions)
                    {
#if DEBUG
                        if (trelloCard != null)
                            throw new Exception("Huh?");
#endif
                        analysisWasUpdated = true;
                        var date = createAction.Date;
                        if (date < firstPeriod.Start)
                            period = firstPeriod;
                        else if (date > lastPeriod.End)
                            period = lastPeriod;
                        else
                            period = result[new Period(date, granularity).Start];
                        var cardStatus = createStatus(createAction);
                        trelloCard = new TrelloCard {Id = card.Id, Name = card.Name};
                        if (cardStatus == CardStatus.Doing)
                            trelloCard.DateCommitted = createAction.Date;
                        period.Set(trelloCard, cardStatus, true);
                    }
                    var moveActions =
                        allActions.OfType<UpdateCardMoveAction>()
                            .Where(a => a.Date <= endDate)
                            .Select(a => new MoveCardAction(a))
                            .ToList();
                    moveActions.Sort((a1, a2) => a1.Date < a2.Date ? -1 : a1.Date > a2.Date ? 1 : 0);
                    foreach (var moveAction in moveActions)
                    {
                        analysisWasUpdated = true;
                        var date = moveAction.Date;
                        if (date < firstPeriod.Start)
                            period = firstPeriod;
                        else if (date > lastPeriod.End)
                            period = lastPeriod;
                        else
                            period = result[new Period(moveAction.Date, granularity).Start];
                        var cardStatus = moveStatus(moveAction);
                        trelloCard = trelloCard ?? new TrelloCard(moveAction.MoveAction.Data.Card);
                        switch (cardStatus)
                        {
                            case CardStatus.Doing:
                                if (!trelloCard.IsCommitted)
                                    trelloCard.DateCommitted = moveAction.Date;
                                break;
                            case CardStatus.Done:
                                if (!trelloCard.IsDone)
                                    trelloCard.DateDone = moveAction.Date;
                                break;
                        }
                        period.Set(trelloCard, cardStatus, true);
                    }
                    UI?.UpdateBusy(++progress);
                }
                if (!analysisExists || analysisWasUpdated)
                    db.SaveBoardAnalysis(_boardAnalysis);
                return _boardAnalysis;
            }
            finally
            {
                UI?.HideBusy();
            }

        }

        private DateTime projectStartTime()
        {
            //var nisse = _trello.Members.ForBoard(Board).ToList(); 
            var lists = _trello.Lists.ForBoard(Board).ToList();
            var startDate = lists.Min(l => l.Id.DateCreated());
            return startDate;
        }

        private PeriodCardsStatus generatePeriods(DateTime start, ref DateTime end, TimeGranularity granularity,
            bool analysisExists, out PeriodCardsStatus lastPeriod, out Dictionary<DateTime, PeriodCardsStatus> result)
        {
            PeriodCardsStatus firstPeriod;
            PeriodCardsStatus period;
            if (analysisExists)
            {
                firstPeriod = _boardAnalysis.Periods.First();
                period = _boardAnalysis.Periods.Last();
            }
            else
            {
                firstPeriod = new PeriodCardsStatus(_boardAnalysis, start, granularity);
                period = firstPeriod;
            }
            result = new Dictionary<DateTime, PeriodCardsStatus> {{firstPeriod.Start, firstPeriod}};
            while (true)
            {
                period = (PeriodCardsStatus) period.Next();
                _boardAnalysis.Periods.Add(period);
                result.Add(period.Start, period);
                if (period.End < end)
                    continue;

                lastPeriod = period;
                end = lastPeriod.End;
                break;
            }
            return firstPeriod;
        }

        private bool tryQuickAnalyzePeriodThroughActions(out ICardActionsProvider cardActionsProvider)
        {
            try
            {
                var since = new SinceDate(_boardAnalysis.Timestamp);
                var allActions = _trello.Actions.AutoPaged().ForBoard(Board, since: since).ToList();
                var dictActions = new Dictionary<CardName, List<Action>>();
                foreach (var action in allActions)
                {
                    List<Action> cardActions;
                    var createAction = action as CreateCardAction;
                    if (createAction != null)
                    {
                        if (!dictActions.TryGetValue(createAction.Data.Card, out cardActions))
                            dictActions.Add(createAction.Data.Card, cardActions = new List<Action>());
                        cardActions.Add(action);
                        continue;
                    }
                    var moveAction = action as UpdateCardMoveAction;
                    if (moveAction == null) continue;
                    if (!dictActions.TryGetValue(moveAction.Data.Card, out cardActions))
                        dictActions.Add(moveAction.Data.Card, cardActions = new List<Action>());
                    cardActions.Add(action);
                }
                cardActionsProvider = new CardActionsProvider(dictActions);
                return true;
            }
            catch
            {
                cardActionsProvider = null;
                return false;
            }
        }


        public IEnumerable<CardId> GetCardsInListsInTimeframe(DateTime start, DateTime end, CardStatus status)
        {
            string[] listIds;
            switch (status)
            {
                case CardStatus.Other:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);

                case CardStatus.Doing:
                    listIds = _doingListsIds;
                    break;

                case CardStatus.Done:
                    listIds = _doneListsIds;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }

            var nisse =
                _trello.Actions.ForCard(new CardId("57dfe7fb3ef9ee3a31263c23"))
                    .OfType<UpdateCardMoveAction>()
                    .Select(a => new MoveCardAction(a))
                    .ToList();

            nisse.Sort((a1, a2) =>
            {
                if (a1.CardId == a2.CardId)
                    return a1.Date < a2.Date ? -1 : a1.Date > a2.Date ? 1 : 0;
                return string.CompareOrdinal(a1.CardId, a2.CardId);
            });

            var cardsInLists = new Dictionary<int, CardId>();
            var allLists = _trello.Lists.ForBoard(Board);
            var actions = new Dictionary<int, MoveCardAction>();
            foreach (var listId in allLists)
            {
                var list = new ListId(listId.Id);
                var listMoves = _trello.Actions.ForList(list).OfType<UpdateCardMoveAction>().Where(a => a.Date <= end).Select(a => new MoveCardAction(a)).ToList();
                foreach (var move in listMoves)
                {
                    var key = move.GetHashCode();
                    if (!actions.ContainsKey(key))
                        actions.Add(key, move);
                }
            }





            var moves = actions.Values.ToList();
            moves.Sort((a1, a2) =>
            {
                if (a1.CardId == a2.CardId)
                    return a1.Date < a2.Date ? -1 : a1.Date > a2.Date ? 1 : 0;
                return string.CompareOrdinal(a1.CardId, a2.CardId);
            });
            foreach (var move in moves)
            {
                var card = new CardId(move);
                var key = card.GetHashCode();
                if (listIds.Any(id => id == move.ListAfter.Id))
                {
                    if (!cardsInLists.ContainsKey(key))
                        cardsInLists.Add(key, card);
                }
                else if (move.Date < start && cardsInLists.ContainsKey(key))
                    cardsInLists.Remove(key);
            }
            return cardsInLists.Values;
        }

        //private static bool sequenceWasCardInListsAtTime(string[] listIds, DateTime start, DateTime end, List<UpdateCardMoveAction> actions, out Card card)
        //{
        //    var moveSequence = new List<UpdateCardMoveAction>(actions.OfType<UpdateCardMoveAction>().Where(a => a.Date <= end));
        //    moveSequence.Sort((a1, a2) => a1.Date < a2.Date ? -1 : a1.Date > a2.Date ? 1 : 0);
        //    var wasInList = false;
        //    foreach (var action in moveSequence)
        //    {
        //        var listId = action.Data.ListAfter.Id;
        //        if (listIds.Any(id => id == listId))
        //        {
        //            wasInList = true;
        //        }
        //        else if (action.Date < start)
        //            wasInList = false;throw
        //    }
        //    card = null;
        //    return wasInList;
        //}

        private Exception error(string msg)
        {
            return error(new Exception(msg));
        }

        private Exception error(Exception ex)
        {
            UI?.Error(ex);
            return ex;
        }

        private void ensureBoardIsSelected()
        {
            if (Board == null)
                throw error("Please select a board.");
        }

        private void ensureProgressListsAreSpecified()
        {
            if (_doingListsIds == null || _doneListsIds == null)
                throw error("Please specify progress ('doing' & 'done') lists.");
        }
    }

    internal interface ICardActionsProvider
    {
        List<Action> GetCardActions(Card cardName);
    }

    class CardActionsReader : ICardActionsProvider
    {
        private readonly ITrello _trello;
        private readonly ISince _since;
        private readonly DateTime _end;

        public List<Action> GetCardActions(Card card)
        {
            return _trello.Actions.AutoPaged()
                .ForCard(new CardId(card.Id), new[] { ActionType.CreateCard, ActionType.UpdateCard }, _since)
                .Where(a => a.Date <= _end)
                .ToList();
        }

        public CardActionsReader(ITrello trello, ISince since, DateTime end)
        {
            _trello = trello;
            _since = since;
            _end = end;
        }
    }

    class CardActionsProvider : ICardActionsProvider
    {
        private readonly Dictionary<CardName, List<Action>> _dictActions;

        public CardActionsProvider(Dictionary<CardName, List<Action>> dictActions)
        {
            _dictActions = dictActions;
        }

        public List<Action> GetCardActions(Card card)
        {
            List<Action> list;
            var cardName = new CardName();
            cardName.AssignShallowFrom(card);
            return _dictActions.TryGetValue(cardName, out list) ? list : new List<Action>();
        }
    }
}
