using TrelloNet;

namespace Trello.net.api
{
    internal class ListIdProvider : IListId
    {
        private readonly string _listId;

        public string GetListId() => _listId;

        public ListIdProvider(string listId)
        {
            _listId = listId;
        }
    }
}