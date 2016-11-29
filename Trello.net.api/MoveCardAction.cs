using System;
using TrelloNet;

namespace Trello.net.api
{
    public class MoveCardAction
    {
        private readonly string _cardId;

        private readonly DateTime _date;

        private readonly string _fromListId;

        private readonly string _toListId;

        public UpdateCardMoveAction MoveAction { get; }

        public DateTime Date => _date;

        public ListName ListAfter => MoveAction.Data.ListAfter;

        public string CardName => MoveAction.Data.Card.Name;

        public string CardId => MoveAction.Data.Card.Id;

        protected bool Equals(MoveCardAction other)
        {
            return string.Equals(_cardId, other._cardId) && _date.Equals(other._date) && string.Equals(_fromListId, other._fromListId) && string.Equals(_toListId, other._toListId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MoveCardAction) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _cardId?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ _date.GetHashCode();
                hashCode = (hashCode*397) ^ (_fromListId?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (_toListId?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        public static bool operator ==(MoveCardAction left, MoveCardAction right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(MoveCardAction left, MoveCardAction right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return $"[{MoveAction.Date:yy-MM-dd hh:mm}] {MoveAction.Data.Card.Name} :: {moveText()}";
        }

        private string moveText() => $"{MoveAction.Data.ListBefore.Name} >> {MoveAction.Data.ListAfter.Name}";

        public MoveCardAction(UpdateCardMoveAction move)
        {
            _cardId = move.Data.Card.Id;
            _date = move.Date;
            _fromListId = move.Data.ListBefore.Id;
            _toListId = move.Data.ListAfter.Id;
            MoveAction = move;
        }
    }
}