using GalaSoft.MvvmLight;

namespace Example_Resources.Model
{
    public class Item : ObservableObject
    {
        private string _name;
        private string _quantity;
        private bool _received;
        private string _website;

        public Item()
        {
        }

        public Item(string quantity, string name, bool received, string website)
        {
            _quantity = quantity;
            _name = name;
            _received = received;
            _website = website;
        }

        public string Quantity
        {
            get => _quantity;
            set { Set(() => Quantity, ref _quantity, value); }
        }

        public string Name
        {
            get => _name;
            set { Set(() => Name, ref _name, value); }
        }

        public bool Received
        {
            get => _received;
            set { Set(() => Received, ref _received, value); }
        }

        public string Website
        {
            get => _website;
            set { Set(() => Website, ref _website, value); }
        }
    }
}