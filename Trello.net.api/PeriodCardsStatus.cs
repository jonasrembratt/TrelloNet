using System;
using System.Collections.Generic;
using System.Linq;
using TrelloNet;
using Tetrapak.ToCommon;

namespace Trello.net.api
{
    public class BoardAnalysisResult
    {
        public string BoardId { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public TimeGranularity Granularity { get; set; }

        public DateTime Timestamp { get; set; }

        public HashSet<TrelloCard> Cards { get; } = new HashSet<TrelloCard>();

        public List<PeriodCardsStatus> Periods { get; } = new List<PeriodCardsStatus>();

        public BoardAnalysisResult(TimeGranularity granularity)
        {
            Granularity = granularity;
        }

        #region .  Equality  .
        public bool Equals(DateTime start, DateTime end, TimeGranularity granularity)
        {
            return Start.Equals(start) && End.Equals(end) && Granularity == granularity;
        }

        protected bool Equals(BoardAnalysisResult other)
        {
            return Equals(other.Start, other.End, other.Granularity);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((BoardAnalysisResult)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Start.GetHashCode();
                hashCode = (hashCode * 397) ^ End.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)Granularity;
                return hashCode;
            }
        }

        public static bool operator ==(BoardAnalysisResult left, BoardAnalysisResult right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(BoardAnalysisResult left, BoardAnalysisResult right)
        {
            return !Equals(left, right);
        } 
        #endregion

        public BoardAnalysisResult(string boardId, DateTime start, DateTime end, TimeGranularity timeGranularity)
        {
            Timestamp = DateTime.Now;
            Start = start;
            End = end;
            Granularity = timeGranularity;
            BoardId = boardId;
        }

        // ReSharper disable once UnusedMember.Global
        public BoardAnalysisResult()
        {
        }

        public bool ContainsCardWithId(string id)
        {
            return Cards.Any(c => c.Id == id);
        }

        public void LinkPeriods()
        {
            PeriodCardsStatus prev = null;
            foreach (var period in Periods)
            {
                period.SetParent(this);
                period.SetPrevious(prev);
                prev?.SetNext(period);
                prev = period;
            }
        }
    }

    public class PeriodCardsStatus : Period
    {
        private BoardAnalysisResult BoardAnalysis { get; set; }

        public HashSet<TrelloCard> Other { get; } = new HashSet<TrelloCard>();

        public HashSet<TrelloCard> Doing { get; } = new HashSet<TrelloCard>();

        public HashSet<TrelloCard> Done { get; } = new HashSet<TrelloCard>();

        public void Set(TrelloCard card, CardStatus status, bool propagateToNext = false)
        {
            if (!BoardAnalysis.Cards.Contains(card))
                BoardAnalysis.Cards.Add(card);
            switch (status)
            {
                case CardStatus.Other:
                    if (Other.Contains(card))
                        break;

                    Other.Add(card);
                    removeFrom(card, Doing, Done);
                    break;

                case CardStatus.Doing:
                    if (Doing.Contains(card))
                        break;

                    Doing.Add(card);
                    removeFrom(card, Done, Other);
                    break;

                case CardStatus.Done:
                    if (Done.Contains(card))
                        break;

                    Done.Add(card);
                    removeFrom(card, Other, Doing);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
            if (propagateToNext)
                ((PeriodCardsStatus)Next(false))?.Set(card, status);
        }

        public IEnumerable<CardName> Started => getStartedInPeriod();

        public IEnumerable<CardName> Closed => getClosedInPeriod();

        public IEnumerable<CardName> StillOngoing => getStillOngoing();

        private IEnumerable<CardName> getStillOngoing()
        {
            var prev = Previous() as PeriodCardsStatus;
            return prev == null
                ? (IEnumerable<CardName>)new CardName[0]
                : Doing.Where(card => prev.Doing.Contains(card)).ToList();
        }

        private IEnumerable<CardName> getStartedInPeriod()
        {
            var prev = Previous() as PeriodCardsStatus;
            return prev == null
                ? (IEnumerable<CardName>)new CardName[0]
                : Doing.Where(card => !prev.Doing.Contains(card)).ToList();
        }

        private IEnumerable<CardName> getClosedInPeriod()
        {
            var prev = Previous() as PeriodCardsStatus;
            return prev == null
                ? (IEnumerable<CardName>) new CardName[0]
                : Done.Where(card => prev.Doing.Contains(card)).ToList();
        }

        public override string ToString()
        {
            return $"{base.ToString()} (Ot={Other.Count}; Dg={Doing.Count}; Dn={Done.Count})";
        }

        protected override Period OnMakeNext(DateTime start, TimeGranularity granularity)
        {
            return new PeriodCardsStatus(BoardAnalysis, start, granularity);
        }

        private static void removeFrom(TrelloCard card, params HashSet<TrelloCard>[] sets)
        {
            foreach (var hashSet in sets)
            {
                if (hashSet.Contains(card))
                    hashSet.Remove(card);
            }
        }

        public void SetParent(BoardAnalysisResult parent)
        {
            BoardAnalysis = parent;
        }

        public PeriodCardsStatus(BoardAnalysisResult boardAnalysis, DateTime start, TimeGranularity granularity)
            : base(start, granularity)
        {
            BoardAnalysis = boardAnalysis;
        }

        // ReSharper disable once UnusedMember.Global
        public PeriodCardsStatus() 
            // ReSharper disable once RedundantBaseConstructorCall
            : base()
        {
        }
    }

    public class TrelloCard : CardName
    {
        private DateTime _dateDone;
        private DateTime _dateCommitted;
        public bool IsCommitted { get; set; }

        public bool IsDone { get; set; }

        public DateTime DateCommitted
        {
            get { return _dateCommitted; }
            set
            {
                _dateCommitted = value;
                IsCommitted = value != default(DateTime);
            }
        }

        public DateTime DateDone
        {
            get { return _dateDone; }
            set
            {
                _dateDone = value;
                IsDone = value != default(DateTime);
            }
        }

        public TimeSpan LeadTime => getLeadTime();

        private TimeSpan getLeadTime()
        {
            return IsCommitted && IsDone 
                ? DateDone.Subtract(DateCommitted) 
                : TimeSpan.Zero;
        }


        public TrelloCard(CardName cardName)
        {
            this.AssignShallowFrom(cardName);
        }

        public TrelloCard()
        {
        }
    }
}