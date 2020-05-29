using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;

namespace Example_Resources.ViewModel
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

            SimpleIoc.Default.Register<ExampleViewModel>();
            SimpleIoc.Default.Register<MainViewModel>();
        }

        public ExampleViewModel ExampleViewModel => ServiceLocator.Current.GetInstance<ExampleViewModel>();

        public MainViewModel MainViewModel => ServiceLocator.Current.GetInstance<MainViewModel>();
    }
}