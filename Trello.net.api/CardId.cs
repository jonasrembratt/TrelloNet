using System;
using TrelloNet;

namespace Trello.net.api
{
    public class CardId : ICardId
    {
        private readonly string _cardId;
        private readonly int _hashCode;

        public CardName Card { get; private set; }

        public string GetCardId() => _cardId;

        public override int GetHashCode()
        {
            return _hashCode;
        }

        public override string ToString()
        {
            return Card.Name;
        }

        public CardId(CardName card)
        {
            if (card == null) throw new ArgumentNullException(nameof(card));
            _cardId = card.Name;
            _hashCode = _cardId.GetHashCode();
            Card = card;
        }

        public CardId(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));
            _cardId = id;
        }

        public CardId(MoveCardAction action)
            : this(action.MoveAction.Data.Card)
        {
            LastAction = action.MoveAction;
        }

        public UpdateCardMoveAction LastAction { get; }
    }
}