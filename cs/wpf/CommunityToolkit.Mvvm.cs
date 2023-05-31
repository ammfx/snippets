// /r:CommunityToolkit.Mvvm
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

public partial class MainWindowVM : ObservableObject
{
    [ObservableProperty]
    string _title = "";

    // optionally implement partial methods:
    // void OnTitleChanging(string value) { }
    // void OnTitleChanged(string value) { }

    // Instead of:
    // string _title = "";
    // public string Title
    // {
    //     get => _title;
    //     set => SetProperty(ref _title, value)
    // }

    // Or even:
    // string _title = "";
    // public string Title
    // {
    //     get => _title;
    //     set
    //     {
    //         if (value == _title) return;
    //         OnTitleChanging(value);
    //         PropertyChanging?.Invoke(this, new PropertyChangingEventArgs(nameof(Title)));
    //         _title = value;
    //         OnTitleChanged(value);
    //         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
    //     }
    // }

    public bool CanSave => true;
    [RelayCommand(CanExecute = nameof(CanSave))]
    async Task OnSaveAsync() { }

    // Instead of:
    // ctor {
    //     SaveCommand = new AsyncRelayCommand(OnSaveAsync, () => CanSave);
    // }
    // IAsyncRelayCommand SaveCommand { get; }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Title))]
    [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
    string _title2 = "";

    // Instead of:
    // string _title2;
    // public string Title2
    // {
    //     get => _title2;
    //     set
    //     {
    //         if (!SetProperty(ref _title2, value)) return;
    //         OnPropertyChanged(nameof(Title));
    //         SaveCommand.NotifyCanExecuteChanged();
    //     }
    // }
}
