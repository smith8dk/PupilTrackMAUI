namespace PupilTrack;

public partial class HGNDetailsPage : ContentPage
{
	public HGNDetailsPage()
	{
		InitializeComponent();
	}

	private async void HGNDetailsBackClicked(object sender, EventArgs e)
	{
    	await Navigation.PushAsync(new HomePage());
	}

    private async void HGNDetailsTestClicked(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new HGNTestPage());
    }

    private async void HGNDetailsResultsClicked(object sender, EventArgs e)
	{
    	await Navigation.PushAsync(new HGNResultsPage());
	}

    private void Label_HandlerChanged(object sender, EventArgs e)
    {
    }
}