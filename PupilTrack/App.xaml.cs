namespace PupilTrack;

public partial class App : Application
{
    
    public Page HomePage { get; private set; }

    public App()
    {
        InitializeComponent();

        
        HomePage = new AppShell();
        
        HomePage = HomePage;

        HomePage = new NavigationPage(new HomePage());
    }
}