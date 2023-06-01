## ChangePropertyAction, InvokeCommandAction
/r:[Microsoft.Xaml.Behaviors.Wpf](https://www.nuget.org/packages/Microsoft.Extensions.Hosting/)
```xml
<ResourceDictionary
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:i="http://schemas.microsoft.com/xaml/behaviors">

  <DataTemplate DataType="{x:Type vm:HistoryTabVM}">
    <TreeView x:Name="historyTree">

      <TreeView.Resources>
        <HierarchicalDataTemplate DataType="{x:Type vm:TaskVM}" ItemsSource="{Binding Children}">
          <StackPanel Orientation="Horizontal">
            <TextBlock x:Name="title" Text="{Binding Title}" />

            <i:Interaction.Triggers>
              <i:EventTrigger EventName="PreviewMouseRightButtonDown">
                <i:ChangePropertyAction PropertyName="IsSelected" Value="True" TargetObject="{Binding}"/>
              </i:EventTrigger>
            </i:Interaction.Triggers>
          </StackPanel>
        </HierarchicalDataTemplate>
      </TreeView.Resources>

      <i:Interaction.Triggers>
        <i:EventTrigger EventName="SelectedItemChanged">
          <i:InvokeCommandAction Command="{Binding NodeSelectedCommand}"
                                 CommandParameter="{Binding SelectedItem, ElementName=historyTree}" />
        </i:EventTrigger>
        <i:EventTrigger EventName="MouseDoubleClick">
          <i:InvokeCommandAction Command="{Binding NodeDoubleClickCommand}" />
        </i:EventTrigger>
      </i:Interaction.Triggers>
    </TreeView>
  </DataTemplate>
</ResourceDictionary>
```
```cs
public partial class HistoryTabVM : ObservableObject
{
    [ObservableProperty]
    ObservableObject? _selectedHistoryNode;

    [RelayCommand]
    void OnNodeSelected(HistoryTabNodeVM nodeVM) => SelectedHistoryNode = nodeVM;

    public event Action<TaskVM>? OnTaskDoubleClick;
    [RelayCommand]
    void OnNodeDoubleClick()
    {
        if (SelectedHistoryNode is TaskXVM vm)
            OnTaskDoubleClick?.Invoke(vm.Task);
    }
}
```
