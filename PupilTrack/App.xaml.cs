namespace PupilTrack;

public partial class App : Application
{
    
    public Page HomePage { get; private set; }

    public App()
    {
        InitializeComponent();

        
        HomePage = new AppShell();

        MainPage = new NavigationPage(new HomePage());
    }
}