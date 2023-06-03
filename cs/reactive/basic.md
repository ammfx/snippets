## Why
Let's say we have stream of events/data. There are 2 ways to get those events:
1. `pull` - periodically fetch from source, like:
   - call GET-url every second
   - or iterate `IEnumerable` using `foreach`
   - or check object property (or compare objects graphs) to detect changes
2. `push` - subscribe to source (by giving it callback) and receive events/changes right when they occur, like:
   -  receive events from web server using WebSockets
   -  or subscribe to event: `button.Click += OnButtonClick;` / `vm.PropertyChanged += OnPropertyChanged;`
   -  or subscribe to Task completion: `task.ContinueWith(t => {...});`
   -  or create in spreadsheet cells with formulas, and change cells values they depend on

When you keep receiving these events you may need typical operations over this stream, like:
- be notified when end of stream reached (completed successfully or with error)
- merge multiple streams, split them in specific ways
- multicast to multiple subscribers full stream or from now
- use LINQ transforms over stream (Select,SelectMany,Where,Group,Join)
- aggregate events for period of time (sum/average/aggregate), skip duplicates, select only 1 event per second, store buffer of N recent events etc
- process events concurrently or dispatch them on specific (ui) thread or thread-pool
- have standard interfaces/implementations for sources/operations to interact with other libs that use the same approach
  - https://github.com/reactivemarbles/DynamicData
  - https://github.com/reactiveui/reactiveui

https://github.com/dotnet/reactive is collection of interfaces/implementations of these typical operations to create & handle `push`-style events streams
```xml
<PackageReference Include="System.Reactive" Version="6.0.0" />
```
Extended packages:
```xml
<!-- implementation of Rx for IAsyncObservable<T> offering deeper async/await support -->
<PackageReference Include="System.Reactive.Async" Version="6.0.0-alpha.18" />
<!-- extended LINQ operators for IAsyncEnumerable and IEnumerable -->
<PackageReference Include="System.Interactive" Version="6.0.1" />
<PackageReference Include="System.Interactive.Async" Version="6.0.1" />
<PackageReference Include="System.Linq.Async" Version="6.0.1" />
```
```cs
interface IObservable<out T> { // events source/stream
    IDisposable Subscribe(IObserver<T> observer);
}
interface IObserver<in T> { // callback
    void OnNext(T value);
    void OnCompleted();
    void OnError(Exception ex);
}
```
```cs
// Requires /r:System.Reactive.Async
interface IAsyncObservable<out T> { // events source/stream
    ValueTask<IAsyncDisposable> SubscribeAsync(IAsyncObserver<T> observer);
}
interface IAsyncObserver<in T> { // callback
    ValueTask OnNextAsync(T value);
    ValueTask OnCompletedAsync();
    ValueTask OnErrorAsync(Exception ex);
}
```
## Create source from event
'Hot' observables, just connected to events stream. Sends to each observer only events that occurred after this observer Subscribe()'d.
```cs
class Button
{
    readonly int _add;
    public Button(int add) { _add = add; }
    public event EventHandler<int>? Click;
    public event Action<int>? ClickInt;
    public event Action? ClickVoid;
    public void DoClick(int i) => Click?.Invoke(this, i + _add);
}
void FromEvent(Button btn)
{
    IObservable<int> clicksInt = Observable.FromEvent<int>(
        h => btn.ClickInt += h, h => btn.ClickInt -= h);

    IObservable<Unit> clicksVoid = Observable.FromEvent( // Unit = void
        h => btn.ClickVoid += h, h => btn.ClickVoid -= h);
}
void FromEventHandler(Button btn)
{
    IObservable<int> clicks1 = Observable.FromEventPattern<int>(
        h => btn.Click += h, h => btn.Click -= h)
        .Select(e => e.EventArgs);

    IObservable<int> clicks2 = Observable.FromEventPattern<int>(
        btn, nameof(btn.Click))
        .Select(e => e.EventArgs);

    using var unsub1 = clicks1.Subscribe(Console.WriteLine);
    btn.DoClick(5);
}
```
## Create source manually
'Cold' observables, re-emits values for each new observer on Subscribe()
```cs
void ForTesting()
{
    Observable.Return(42);
    Observable.Empty<int>(); // just completes
    Observable.Never<int>(); // never completes
    Observable.Throw<Exception>(new Exception("42"));
    Observable.Range(1, 10);
    Observable.FromAsync(async () => {...}); // supports CancellationToken
    Observable.Delay(lines, TimeSpan / DateTimeOffset);
    Observable.Timer(TimeSpan delay / DateTimeOffset at); // == Delay(), 1 value
    Observable.Interval(TimeSpan); // 1 value periodically
    Observable.Timer(TimeSpan delay, TimeSpan interval); // 1 value periodically
}
void FromToIEnumerable()
{
    var list = new[] {1, 2, 5};
    IObservable<int> streamedInts = ...;
    IObservable<int> merged = list.ToObservable().Concat(streamedInts);
    // var list2 = merged.ToEnumerable();
    // ToDictionary()/ToLookup() - sequence squashed when subscribed
}
void Generators()
{
    IObservable<int> squares = Observable.Generate(0, i => i < 10, i => i + 1, i => i * i);

    var lines = Observable.Using(() => File.OpenText("file.txt"), reader =>
                   Observable.Generate(reader, x => !x.EndOfStream, x => x, x => x.ReadLine()));
}
void FromTask()
{
    Task<int> GetIntAsync() { }
    IObservable<int> observableTask = GetIntAsync().ToObservable();
}
void Factory()
{
    // calls factory when each new observer subscribes
    IObservable<string> lines = Observable.Defer(() => {
        var list = Fetch(); return list.ToObservable();
    });
}
void Create()
{
    // Executes delegate only (and on each) .Subscribe()
    var squares = Observable.Create<int>(observer => {
        for (int i = 0; i < 10; i++)
            observer.OnNext(i * i);
        observer.OnCompleted();
        return Disposable.Empty;
    });
    var squaresAsyncWork = Observable.Create<int>(async (observer, ct) => {
        var res = await DoWork();
        for (int i = 0; i < 10; i++) {
            ct.ThrowIfCancellationRequested();
            observer.OnNext(i * i);
        }
        observer.OnCompleted();
        // return Disposable.Empty; // Optional for async methods
    });
}
void CreateOnThreadPool()
{
    var squares = Observable.Create<int>(observer => {
        Task.Run(() => {
            for (int i = 0; i < 10; i++) {
                ct.ThrowIfCancellationRequested();
                observer.OnNext(i * i);
            }
            observer.OnCompleted();
        }, ct);
    });
    var squares = Observable.Create<int>(observer => {
        var cts = new CancellationTokenSource();
        Task.Run(() => {
            for (int i = 0; i < 10; i++) {
                cts.Token.ThrowIfCancellationRequested();
                observer.OnNext(i * i);
            }
            observer.OnCompleted();
        }, cts.Token);
        return new CancellationDisposable(cts);
        // v2) return () => cts.Cancel();
    });
}
class Squares : IObservable<int> // better use ObservableBase<T>
{
    public IDisposable Subscribe(IObserver<int> observer) {
        for (int i = 0; i < 10; i++)
            observer.OnNext(i * i);
        observer.OnCompleted();
        return Disposable.Empty;
    }
}
class ButtonClicksObservable : ObservableBase<int>
{
    readonly Button _btn;
    public ButtonClicksObservable(Button btn) { _btn = btn; }
    protected override IDisposable SubscribeCore(IObserver<int> observer)
    {
        void OnButtonClick(object? sender, int e) => observer.OnNext(e);
        _btn.Click += OnButtonClick;
        return Disposable.Create(() => _btn.Click -= OnButtonClick);
    }
}
// Creating extension methods that wrap creating observables/transformers/observers are useful
static class Exts
{
    public static IObservable<int> ToObservable(this Button btn) =>
        new ButtonClicksObservable(btn);
}
```
## Disposables
```cs
void Disposables()
{
    Disposable.Empty; // AsyncDisposable.Nop
    Disposable.Create(() => {...}); // // AsyncDisposable.Create(() => { ValueTask });
    new CompositeDisposable(IDisposable[]); // CompositeAsyncDisposable()
    StableCompositeDisposable.Create(IDisposable,IDisposable);
    new CancellationDisposable(CancellationTokenSource); // CancellationAsyncDisposable()
    new ContextDisposable(SynchronizationContext, IDisposable); // disposed on context
    new ScheduledDisposable(IScheduler, IDisposable); // disposed on scheduler

    new BooleanDisposable(); // IsDisposed
    new MultipleAssignmentDisposable(); // IDisposable? Disposable {get;set;}
    new RefCountDisposable(IDisposable underlying); // GetDisposable()/Release()
    new SerialDisposable();
    new SingleAssignmentDisposable();
}
```
## Create observer
```cs
void Test() {
    // Create observer just from OnNext delegate.
    // Can also use overload with OnComplete,onError
    // In 'using' to Dispose() subscription
    using var subscription = lines.Subscribe(line => Console.WriteLine(line));

    // cancel Token to Dispose() subscription
    var cts = new CancellationTokenSource();
    lines.Subscribe(line => Console.WriteLine(line), cts.Token);
}
class ConsoleObserver<T> : IObserver<T> { ... }

class ConsoleObserver : ObserverBase<int> {
    protected override void OnNextCore(int value) => Console.WriteLine(value);
    protected override void OnCompletedCore() => Console.WriteLine("Complete");
    protected override void OnErrorCore(Exception ex) => Console.WriteLine(ex);
}
static class Exts {
    public static IDisposable SubscribeConsole<T>(this IObservable<T> source) =>
        source.Subscribe(new ConsoleObserver<T>());
}
```
## Subject
Replicates events from multiple Observables to multiple Observers
```cs
interface IConnectableObservable<out T> : IObservable<T> {
    IDisposable Connect();
}
void Subject_Test()
{
    var subj = new Subject<string>();
    lines1.Subscribe(subj); // source1
    lines2.Subscribe(subj); // source2, optionally
    // subj completes when any of source1/source2 completes
    subj.Subscribe(x => Console.WriteLine(x)); // observer 1
    subj.Subscribe(x => {...}); // observer 2
    // independent on number of subj.Subscribe() calls - lines1.Subscribe() called only once
    // will stream `lines1` events to every subscriber
    subj.OnNext("from subj");   // can call OnNext()/.. directly too

    // == Multicast(Subject)
    IConnectableObservable<string> subj = lines.Publish();
    subj.Subscribe(x => {...});
    subj.Subscribe(x => {...});
    subj.Connect();
    subj.Subscribe(x => {...}); // receives only events from this moment, not all `lines`
    // any subscriber that missed lines will receive nothing.
    // to receive last event use PublishLast()

    // iterate/reuse `published` stream multiple times to generate result stream
    // `published` it will return same events from `lines`
    IObservable<string> res = lines.Publish(published => ...);

    var linesFactory = Observable.Defer(() => { Fetch(); return lines; });
    var sharedLines = linesFactory.Publish();
    sharedLines.Subscribe(...);
    sharedLines.Subscribe(...);
    // will call Fetch() only once
    using var subscription = sharedLines.Connect();

    // pattern to unsubscribe `subj` from `sharedLines`
    // when all `subj` subscribers unsubscribed
    var subj = sharedLines.Publish().RefCount();
}
void AsyncSubject_Test()
{
    // caches 1 event & sends is to each new observer
    var taskSubj = new AsyncSubject<int>();
    // call {taskSubj.OnNext(t.Result); taskSubj.OnCompleted();} on Task completion

    // == Multicast(AsyncSubject)
    IConnectableObservable<string> subj = lines.PublishLast();

    IObservable<string> res = lines.PublishLast(published => ...);
}
void BehaviorSubject_Test()
{
    // caches last event & sends it to each new observer
    var statusStream = new BehaviorSubject<string>(); // .Value

    // == Multicast(BehaviorSubject)
    // observers that were subscribed before Connect() (before `lines` emitted value)
    // will receive initialValue, and those subscribed later receive last value from `lines`
    IConnectableObservable<string> subj = lines.Publish(string initialValue);

    IObservable<string> res = lines.Publish(published => ..., string initialValue);
}
void ReplaySubject_Test()
{
    // caches sequence
    var sequence = new ReplaySubject<int>();
    new ReplaySubject<int>(int bufferSize, TimeSpan window, IScheduler);

    // == Multicast(ReplaySubject)
    lines.Replay(); // replay all events
    lines.Replay(int n / TimeSpan); // caches & replays n last events to new observers
}
void Multicast_Test()
{
    // multicast events through `subj` to each new observer
    // subj could be Subject/AsyncSubject/BehaviorSubject/BehaviorSubject or any custom
    lines.Multicast(ISubject subj);
}
class MyCustomSubject : SubjectBase<int> { }
```
## Operations over IObservable\<T> -> IObservable\<U>
```cs
void LINQ(IObservable<string> lines)
{
    lines.Do(line => {...}); // Subscribe-like side-effects, has overloads with onCompleted/onError

    lines.Select(x => x[0]); // map
    lines.Select((x, index) => x[0]); // map
    lines.Timestamp(); // => IObservable<(DateTime Timestamp, string Value)>

    lines.Where(x => x[0]=='A'); // filter

    lines.Distinct(x => x[0]); // buffers all events!
    lines.DistinctUntilChanged();

    lines.SelectMany(x => x.ToCharArray()); // flatmap
    lines.SelectMany((x, index) => x.ToCharArray()); // flatmap
    // 19 SelectMany overloads that operate on IObservable's,Task's,IEnumerables

    lines.GroupBy(x => x[0]).Select(x => (x.Key, x.Max(y => y.Length)));
}
void AwaitTasks()
{
    lines.SelectMany(x => CheckAsync(x), (x, checkResult) => (x, checkResult)); // awaits tasks
    // same, 2 'from' == SelectMany()
    var filtered = from x in lines
                   from checkResult in CheckAsync(x) // awaits tasks
                   where checkResult select x;

    lines.Select(async x => (x, await CheckAsync(x))).Concat(); // Concat() to keep order

    Observable.Interval(TimeSpan.FromSeconds(1))
        .SelectMany(ticks => CheckAsync())
        .ObserveOnDispatcher()
        .Subscribe(() => {...})); // on UI thread
}
void Merging()
{
    lines.Concat(lines2); // x,A,y,B,z => x,y,z,A,B
    lines.Merge(lines2);  // x,A,y,B,z => x,A,y,B,z
    lines.Zip(lines2, (x,y) => x + y);            // s,x,A,y,B,z => sA,xB
    lines.CombineLatest(lines2, (x,y) => x + y);  // s,x,A,y,B,z => xA,yA,yB,zB
    lines.WithLatestFrom(lines2, (x,y) => x + y); // s,x,A,y,B,z =>
    lines.StartWith(IEnumerable arr); // == arr.ToObservable().Concat(lines)
    lines.Join(lines2, ); // merge using left&right durationSelectors. GroupJoin()
}
void IObservable_of_IObservables()
{
    // flatten sequence of observables (== SelectMany())
    lines.Select(x => /*other observable(x)*/).Merge();
    new[] { lines1, lines2 }.ToObservable().Merge(int maxConcurrentSubscriptions);

    // get events from last available IObservable
    // if next IObservable available - switch to it (and unsubscribe from previous)
    IObservable<int> Switch(this IObservable<IObservable<int>> sequences);

    Observable.Amb(lines1, lines2); // switch to first available of 2
}
void Skip_TakeUntil_Buffer()
{
    lines.Delay(TimeSpan / DateTimeOffset);
    lines.DelaySubscription(TimeSpan / DateTimeOffset).Subscribe(...);

    lines.Skip(int n / TimeSpan); // +IScheduler
    lines.SkipWhile(x => IsSkip);
    lines.SkipUntil(DateTimeOffset)
    lines.SkipUntil(IObservable<U> other); // skip lines until `other` emits 1 value

    lines.Take(int n / TimeSpan); // +IScheduler
    lines.TakeWhile(x => IsContinue);
    lines.TakeUntil(x => IsStop / DateTimeOffset);
    lines.TakeUntil(IObservable<U> other); // unsubscribe when `other` emits 1 value

    lines.TakeLast(int n / TimeSpan); // +IScheduler
    lines.SkipLast(int n / TimeSpan); // +IScheduler

    // n/skip can be IObservables - when buffer opens/closes
    lines.Buffer(int n, int skip / Timespan); // IObservable<IList<string>>
    lines.Window(...); // same as Buffer but     IObservable<IObservable<string>>
    lines.TakeLastBuffer(int n / TimeSpan);   // == Buffer(n,1) sliding buffer

    // create new sequence by emitting event e only if there were
    // no other event occurred in TimeSpan after e
    lines.Throttle(TimeSpan);

    lines.Repeat(); // re-subscribe previous operations indefinitely
    lines.Repeat(int times);
}
void Aggregating()
{
    // == Observable.Return(n), observables emitting single value
    lines.Count(x => x.Length > 10);
    lines.Sum(x => x.Length);
    lines.Average(x => x.Length);        // Min,Max,MinBy,MaxBy
    lines.Aggregate((acc,x) => acc + x); // emit single value
    lines.Scan((acc,x) => acc + x);      // emit all intermediate values
}
```
