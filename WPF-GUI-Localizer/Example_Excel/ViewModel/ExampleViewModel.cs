using System.Collections.ObjectModel;
using Example_Excel.Model;
using GalaSoft.MvvmLight;

namespace Example_Excel.ViewModel
{
    public class ExampleViewModel : ViewModelBase
    {
        public ObservableCollection<Item> Items { get; } = new ObservableCollection<Item>();

        public ExampleViewModel()
        {
            Items.Add(new Item("4", "Printer", true, "https://store.hp.com/us/en/cv/printers"));
            Items.Add(new Item("some", "Mice", false, "https://www.logitech.com/en-us/mice"));
            Items.Add(new Item("100 kg", "Rice", true, "-"));
            Items.Add(new Item("10 gallon", "Milk", false, "-"));
        }
    }
}