namespace PupilTrack;

public partial class HomePage : ContentPage
{
    public HomePage()
    {
        InitializeComponent();
    }

    private async void OnLogoTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new AboutPage());
    }
   
    private async void OnProfileTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ProfilePage());
    }

    private async void OnHGNTestTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HGNDetailsPage());
        //await Shell.Current.GoToAsync(nameof(HGNDetailsPage));
    }

    private async void OnWalkTurnTestTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new WalkTurnDetailsPage());
    }

    private async void OnOneLegTestTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new OneLegDetailsPage());
    }

   private async void OnHelpPageTapped(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HelpPage());
    }

}
