using GalaSoft.MvvmLight;

namespace Example_Excel.Model
{
    public class Item : ObservableObject
    {
        private string _quantity;
        private string _name;
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
            set { Set<string>(() => Quantity, ref _quantity, value); }
        }

        public string Name
        {
            get => _name;
            set { Set<string>(() => Name, ref _name, value); }
        }

        public bool Received
        {
            get => _received;
            set { Set<bool>(() => Received, ref _received, value); }
        }

        public string Website
        {
            get => _website;
            set { Set<string>(() => Website, ref _website, value); }
        }
    }
}