namespace PupilTrack;

public partial class ProfilePage : ContentPage
{
    public ProfilePage()
    {
        InitializeComponent();
    }

    private async void ProfileBackClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HomePage());
    }
}