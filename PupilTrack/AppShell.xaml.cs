namespace PupilTrack;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
        Routing.RegisterRoute(nameof(HGNTestPage), typeof(HGNTestPage));
        Routing.RegisterRoute(nameof(WalkTurnTestPage), typeof(WalkTurnTestPage));
        Routing.RegisterRoute(nameof(OneLegTestPage), typeof(OneLegTestPage));
        Routing.RegisterRoute(nameof(HelpPage), typeof(HelpPage));
    }
}
