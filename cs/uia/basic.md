## UIA
Tools for inspecting Windows UIA elements:
- [VisualUIAVerifyNative.exe](https://learn.microsoft.com/en-us/windows/win32/winauto/ui-automation-verify) (in WindowsSDK)
- [Inspect.exe](https://learn.microsoft.com/en-us/windows/win32/winauto/inspect-objects) (in WindowsSDK) *(previously UISpy.exe)*
- https://github.com/snoopwpf/snoopwpf
- [Spyxx.exe](https://learn.microsoft.com/en-us/visualstudio/debugger/how-to-start-spy-increment) (Visual Studio)

TestProject.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net7.0-windows</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.WindowsDesktop.App.WPF"/>
  </ItemGroup>
</Project>
```
Using [Utils.cs](Utils.cs)
```cs
using System.Diagnostics;
using System.Windows.Automation;

void AutomateNotepad()
{
	string savePath = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.Desktop), // %UserProfile%\Desktop
		$"Hello_world_{DateTime.Now:MMdd_hhmmss}.txt");

	var proc = Process.Start("notepad.exe");
	if (!Exts.Wait(() => Exts.IsWindow(proc.MainWindowHandle), TimeSpan.FromSeconds(2))) return;
	var notepadWnd = Exts.Wait(() => Exts.TryFindTopWindow(proc.Id));
	if (notepadWnd == null) return;
	var textArea = notepadWnd.FindChild(ControlType.Document);
	textArea.SetText("Hello world!");

	var menuBar = notepadWnd.FindChild(ControlType.MenuBar);
	var fileItem = menuBar.FindChild(ControlType.MenuItem, name: "File");
	fileItem!.GetPattern<ExpandCollapsePattern>().Expand();
	var fileMenu = fileItem.FindChild(ControlType.Menu);
	var saveItem = fileMenu.FindChild(ControlType.MenuItem, name: "Save");
	saveItem.GetPattern<InvokePattern>().Invoke();

	var saveAsWnd = notepadWnd.FindChild(ControlType.Window, name: "Save As");
	var filenameTxt = saveAsWnd.FindChild(ControlType.Edit, id: "1001", scope: TreeScope.Descendants);
	filenameTxt.SetText(savePath);
	var saveBtn = saveAsWnd.FindChild(ControlType.Button, id: "1");
	saveBtn.GetPattern<InvokePattern>().Invoke();

	notepadWnd.GetPattern<WindowPattern>().Close();
}
```
