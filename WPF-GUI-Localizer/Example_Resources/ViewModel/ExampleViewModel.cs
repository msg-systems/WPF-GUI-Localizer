using System.Collections.ObjectModel;
using Example_Resources.Model;
using GalaSoft.MvvmLight;

namespace Example_Resources.ViewModel
{
    public class ExampleViewModel : ViewModelBase
    {
        public ExampleViewModel()
        {
            Items.Add(new Item("4", "Printer", true, "https://store.hp.com/us/en/cv/printers"));
            Items.Add(new Item("some", "Mice", false, "https://www.logitech.com/en-us/mice"));
            Items.Add(new Item("100 kg", "Rice", true, "-"));
            Items.Add(new Item("10 gallon", "Milk", false, "-"));
        }

        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();
    }
}