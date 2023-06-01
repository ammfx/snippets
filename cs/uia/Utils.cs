using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;

static class Exts
{
    static readonly TimeSpan RetryTimeout = TimeSpan.FromMilliseconds(200);
    static readonly TimeSpan WaitChildTimeout = TimeSpan.FromSeconds(1);

    public static T? Wait<T>(Func<T?> condition, TimeSpan timeout = default)
    {
        var end = DateTime.Now + timeout;
        while (true)
        {
            try
            {
                var res = condition();
                if (!EqualityComparer<T>.Default.Equals(res, default))
                    return res;
                if (DateTime.Now > end) break;
            }
            catch
            {
                if (DateTime.Now > end) throw;
            }
            Thread.Sleep(RetryTimeout);
        }
        return default;
    }

    static readonly Dictionary<Type, AutomationPattern> _patterns = new();
    public static T GetPattern<T>(this AutomationElement ae) where T : BasePattern
    {
        if (!_patterns.TryGetValue(typeof(T), out var p))
            _patterns[typeof(T)] = p = (AutomationPattern)typeof(T).GetField("Pattern", BindingFlags.Static | BindingFlags.Public)!.GetValue(null)!;
        return (T)ae.GetCurrentPattern(p);
    }

    public static AutomationElement? FirstChild(this AutomationElement root)
        => root.FindFirst(TreeScope.Children, Condition.TrueCondition);

    public static AutomationElement GetParent(this AutomationElement ae)
        => TreeWalker.RawViewWalker.GetParent(ae);

    public static Condition GetCondition(ControlType type, string? id = null, string? name = null, string? className = null, int processId = 0)
    {
        var typeCond = new PropertyCondition(AutomationElement.ControlTypeProperty, type);
        if (id == null && name == null && className == null && processId == 0)
            return typeCond;
        var conds = new List<Condition> { typeCond };
        if (processId != 0) conds.Add(new PropertyCondition(AutomationElement.ProcessIdProperty, processId));
        if (id != null) conds.Add(new PropertyCondition(AutomationElement.AutomationIdProperty, id));
        if (name != null) conds.Add(new PropertyCondition(AutomationElement.NameProperty, name));
        if (className != null) conds.Add(new PropertyCondition(AutomationElement.ClassNameProperty, className));
        return new AndCondition(conds.ToArray());
    }

    public static AutomationElement? TryFindTopWindow(int processId, string? id = null, string? title = null) =>
        AutomationElement.RootElement.TryFindChild(ControlType.Window, id, title, null, processId);

    public static AutomationElement FindChild(this AutomationElement root, ControlType type, string? id = null, string? name = null, string? className = null, int processId = 0, TreeScope scope = TreeScope.Children, TimeSpan timeout = default)
    {
        if (timeout == default) timeout = WaitChildTimeout;
        var res = Wait(() => root.TryFindChild(type, id, name, className, processId, scope), timeout)
            ?? throw new ElementNotAvailableException();
        return res;
    }
    public static AutomationElement? TryFindChild(this AutomationElement root, ControlType type, string? id = null, string? name = null, string? className = null, int processId = 0, TreeScope scope = TreeScope.Children)
        => root.FindFirst(scope, GetCondition(type, id, name, className, processId));

    public static string DumpDescendants(this AutomationElement root)
    {
        var sb = new StringBuilder();
        Dump(root, "");
        return sb.ToString();

        void Dump(AutomationElement parent, string indent)
        {
            foreach (AutomationElement child in parent.FindAll(TreeScope.Children, Condition.TrueCondition))
            {
                var c = child.Current;
                sb.AppendLine($"{indent}{c.ControlType.ProgrammaticName},\tId='{c.AutomationId}',\tName='{c.Name}'");
                Dump(child, indent + "\t");
            }
        }
    }

    public static void SetText(this AutomationElement el, string text)
    {
        if (el.TryGetCurrentPattern(ValuePattern.Pattern, out object pattern))
        {
            ((ValuePattern)pattern).SetValue(text);
        }
        else
        {
            _ = SendMessageW(el.Current.NativeWindowHandle, WM_SETTEXT, 0, text);
        }
    }

    public static string? GetSelectionText(this AutomationElement root)
        => root.GetPattern<SelectionPattern>().Current.GetSelection().FirstOrDefault()?.Current.Name;

    [DllImport("user32")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool IsWindow(IntPtr hWnd);

    [DllImport("user32", CharSet = CharSet.Unicode)]
    static extern int SendMessageW(int hWnd, int uMsg, int wParam, string lParam);
    const int WM_SETTEXT = 0x000C;
}
