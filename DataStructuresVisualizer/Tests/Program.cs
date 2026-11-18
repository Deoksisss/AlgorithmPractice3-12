using System.Collections;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using DataStructuresVisualizer;
using DataStructuresVisualizer.Controls;
using DataStructuresVisualizer.Models;
using DataStructuresVisualizer.Services;
using DataStructuresVisualizer.Views;
using StackQueueLab.Core.Structures;

internal static class Program
{
    private static int checks;
    private static Window host = null!;
    private static void Assert(bool value, string message)
    {
        checks++;
        if (!value) throw new Exception(message);
    }
    private static object? Field(object view, string name) => view.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view);
    private static object? Call(object view, string method, params object?[] args) => view.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, args);
    private static void Click(object view, string method) => Call(view, method, null, null);
    private static T Find<T>(Control view, string name) where T : Control => view.FindControl<T>(name)!;
    private static void Pump(int milliseconds = 40)
    {
        using var cancellation = new CancellationTokenSource(milliseconds);
        Dispatcher.UIThread.MainLoop(cancellation.Token);
    }
    private static void Show(Control view)
    {
        host.Content = view;
        host.Width = 1100; host.Height = 700;
        Pump();
    }
    private static void Settle(object view)
    {
        foreach (var field in view.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            if (field.GetValue(view) is SceneMotion motion) motion.Complete();
        Pump();
    }
    private static void Contained(Canvas canvas)
    {
        foreach (var card in canvas.Children.OfType<Border>())
        {
            var x = Canvas.GetLeft(card); var y = Canvas.GetTop(card);
            Assert(double.IsFinite(x + y) && x >= 0 && y >= 0, "Non-finite/negative card position");
            Assert(x + card.Bounds.Width <= canvas.Width + 1 && y + card.Bounds.Height <= canvas.Height + 1, $"Card outside canvas: {canvas.Name} {x},{y} {card.Bounds.Size} scene={canvas.Width},{canvas.Height} opacity={card.Opacity}");
        }
    }

    public static int Main()
    {
        try
        {
            AppBuilder.Configure<App>().UseHeadless(new AvaloniaHeadlessPlatformOptions()).SetupWithoutStarting();
            host = new Window(); host.Show();
            TestServices(); TestStack(); TestQueue(); TestLists(); TestMaze(); TestInfix(); TestTree(); TestBank(); TestViewport();
            host.Close();
            Console.WriteLine($"PASS: {checks} assertions across services and all seven visualizations.");
            return 0;
        }
        catch (Exception e) { Console.Error.WriteLine(e); return 1; }
    }

    private static void TestServices()
    {
        foreach (var expression in new[] { "1", "(2+3)*(5-1)", "8/2/2", "1+2*3-4/2", string.Join("+", Enumerable.Repeat("1", 100)) })
        {
            var conversion = InfixConversionService.ConvertWithSteps(expression);
            Assert(conversion.Postfix == ExpressionService.ToPostfix(expression), "Infix result mismatch");
            var previousOutput = 0;
            foreach (var step in conversion.Steps)
            {
                Assert(step.Output.Count - previousOutput is >= 0 and <= 1, "An output transfer was skipped");
                Assert(step.StackIndices!.Concat(step.OutputIndices!).Distinct().Count() == step.Stack.Count + step.Output.Count, "Token identity duplicated");
                previousOutput = step.Output.Count;
            }
        }
        var extended = InfixConversionService.ConvertWithSteps("sin(0)+sqrt(9)");
        Assert(extended.Postfix == "0 sin 9 sqrt +", "Extended infix conversion");
        Assert(Math.Abs(InfixConversionService.EvaluatePostfix(extended.Postfix) - 3) < 1e-9, "Extended postfix evaluation");
        Assert(InfixConversionService.ConvertWithSteps("2^3^2").Postfix == "2 3 2 ^ ^", "Right-associative power");
        Assert(InfixConversionService.EvaluatePostfix("10 2 :") == 5, "Colon division");
        Assert(InfixConversionService.EvaluatePostfix("2 3 ^ 9 sqrt + 1 - 4 *") == 40, "Extended postfix chain");
        var cache = new LruExpressionCache(4);
        for (var i = 0; i < 5; i++) cache.Put(i.ToString(), i);
        Assert(cache.Count == 4 && cache.Replacements == 1, "LRU eviction");
        cache.TryGet("1", out _);
        Assert(cache.Snapshot()[0].StartsWith("1 ="), "LRU promotion");
        var snapshot = cache.Capture(); cache.Clear(); cache.Restore(snapshot);
        Assert(cache.Count == 4 && cache.Hits == 1 && cache.Replacements == 1, "LRU restore");
        var steps = new List<EvaluationStep>();
        var root = ExpressionService.BuildTree(ExpressionService.ToPostfix("(2+3)*(2+3)"));
        Assert(ExpressionService.Evaluate(root, steps, cache) == 25, "Tree evaluation");
        Assert(steps.Any(s => s.FromCache), "Repeated subtree cache hit");
        Assert(steps[0].CacheEntries!.Count < steps[^1].CacheEntries!.Count || steps[0].CacheState != steps[^1].CacheState, "Cache snapshots all show final state");
        Console.WriteLine("PASS services: token identities, atomic transfers, LRU order/eviction/restore, tree values");
    }

    private static void TestStack()
    {
        var view = new StackDemoView(); Show(view);
        Click(view, "Pop_Click");
        for (var i = 0; i < 60; i++) Call(view, "Push", i.ToString());
        Settle(view);
        var canvas = Find<Canvas>(view, "StackCanvas"); Contained(canvas);
        var stack = (LinkedStack<string>)Field(view, "stack")!;
        Assert(stack.Snapshot()[0] == "59", "Stack LIFO top");
        Click(view, "Pop_Click"); Settle(view);
        Assert(stack.Snapshot()[0] == "58", "Stack LIFO pop");
        Click(view, "Reset_Click"); Settle(view);
        Assert(canvas.Height < 300 && canvas.Children.OfType<Border>().Count() == 0, "Stack reset extents");
        Assert(canvas.Children.OfType<TextBlock>().Any(), "Reset removed top label");
        Console.WriteLine("PASS stack: empty/pop, 60 pushes, LIFO, reset, bounds");
    }

    private static void TestQueue()
    {
        var view = new QueueDemoView(); Show(view); Click(view, "Dequeue_Click");
        for (var i = 0; i < 60; i++) Call(view, "Enqueue", i.ToString());
        Settle(view);
        foreach (var name in new[] { "LinkedQueueCanvas", "StandardQueueCanvas" }) Contained(Find<Canvas>(view, name));
        Click(view, "Dequeue_Click"); Settle(view);
        Assert(((IEnumerable<string>)Field(view, "linkedQueue")!).SequenceEqual(((IEnumerable<string>)Field(view, "standardQueue")!)), "Queue implementations diverged");
        Assert(((IEnumerable<string>)Field(view, "linkedQueue")!).First() == "1", "Queue FIFO");
        Assert(Find<Canvas>(view, "LinkedQueueCanvas").Children.OfType<LinkLayer>().Single().Nodes.Count == 59, "Queue links");
        Click(view, "Reset_Click"); Settle(view);
        Assert(Find<Canvas>(view, "LinkedQueueCanvas").Width < 400, "Queue reset extents");
        Console.WriteLine("PASS queue: empty, 60 elements, FIFO, both models, links, reset");
    }

    private static void TestLists()
    {
        var view = new LinkedListAlgorithmsView(); Show(view); Settle(view);
        var cases = new[] {
            (0,"1 2 3","0","0","3 2 1"), (1,"1 2 3","0","0","3 1 2"),
            (2,"1 2 3","0","0","2 3 1"), (3,"1 2 1","0","0","1 2 1"),
            (4,"1 2 1","0","0","2"), (5,"1 2 3","2","0","1 2 1 2 3 3"),
            (6,"1 3 5","4","0","1 3 4 5"), (7,"1 2 1","1","0","2"),
            (8,"1 2 3","8","2","1 8 2 3"), (0,"","0","0",""),
            (0,"7","0","0","7"), (7,"1 1","1","0",""),
            (6,"3 1","2","0","3 1"), (8,"1 2","8","9","1 2") };
        foreach (var (task,input,a,b,expected) in cases)
        {
            Find<TextBox>(view,"LinkedListValuesInput").Text=input;
            Click(view,"ApplyValues_Click"); Settle(view);
            Find<ComboBox>(view,"TaskCombo").SelectedIndex=task;
            Find<TextBox>(view,"Param1Input").Text=a; Find<TextBox>(view,"Param2Input").Text=b;
            Click(view,"Execute_Click"); Settle(view); Pump(360);
            Assert(Find<TextBox>(view,"LinkedListValuesInput").Text==expected,$"List task {task}: {input}");
            Contained(Find<Canvas>(view,"ListCanvas"));
        }
        Find<TextBox>(view,"LinkedListValuesInput").Text=string.Join(" ", Enumerable.Range(1,80));
        Click(view,"ApplyValues_Click"); Settle(view);
        Find<ComboBox>(view,"TaskCombo").SelectedIndex=0;
        Click(view,"Execute_Click"); Pump(320);
        var cards=Find<Canvas>(view,"ListCanvas").Children.OfType<Border>().ToArray();
        for(var i=0;i<cards.Length;i++) for(var j=i+1;j<cards.Length;j++)
            Assert(!new Rect(Canvas.GetLeft(cards[i]),Canvas.GetTop(cards[i]),66,54).Intersects(new Rect(Canvas.GetLeft(cards[j]),Canvas.GetTop(cards[j]),66,54)), "List overlap during reversal");
        Settle(view); Contained(Find<Canvas>(view,"ListCanvas"));
        Console.WriteLine("PASS lists: every operation, absent value, empty/single, sorted precondition, 80-node intermediate reversal");
    }

    private static void TestMaze()
    {
        var view=new MazeView(); Show(view);
        Find<TextBox>(view,"MazeInput").Text="#####\n#S.E#\n#####";
        Click(view,"ManualLoadButton_Click"); Click(view,"StartButton_Click");
        Assert(((MazeGrid)Field(view,"grid")!).Width == 5, "Manual maze was not loaded");
        Click(view,"StopButton_Click");
        var frames=(IReadOnlyList<MazeFrame>)Field(view,"frames")!;
        foreach(var frame in frames) Call(view,"ApplyFrame",frame);
        var before=Find<Canvas>(view,"MazeCanvas").Children.OfType<Border>().Count(b=>b.Classes.Contains("pathCell"));
        Assert(before>0,"DFS final path missing");
        Call(view,"RenderGrid");
        Assert(Find<Canvas>(view,"MazeCanvas").Children.OfType<Border>().Count(b=>b.Classes.Contains("pathCell"))==before,"DFS resize lost state");
        Click(view,"GenerateButton_Click");
        Assert(Field(view,"timer") is null && Field(view,"frames") is null,"New maze uses stale playback");
        Find<TextBox>(view,"MazeInput").Text="#######\n#S.#.E#\n#######";
        Click(view,"ManualLoadButton_Click"); Click(view,"StartButton_Click");
        var blocked=(IReadOnlyList<MazeFrame>)Field(view,"frames")!;
        Assert(blocked[^1].Type == MazeFrameType.NoPath,"Unreachable maze");
        foreach(var frame in blocked.Where(f=>f.Type==MazeFrameType.PopCurrent))
            Assert(!frame.Stack.Contains(frame.Point),"Popped cell remains in stack snapshot");
        Click(view,"StopButton_Click");
        Console.WriteLine("PASS maze: final path, redraw state, regeneration during playback");
    }

    private static void TestInfix()
    {
        var view=new InfixPostfixView(); Show(view);
        Find<TextBox>(view,"InfixExpressionInput").Text="1+2*3"; Click(view,"Build_Click");
        var conversion=(InfixConversionResult)Field(view,"conversion")!;
        var original=Find<Canvas>(view,"TokenCanvas").Children.OfType<Border>().ToArray();
        foreach(var step in conversion.Steps) { Call(view,"ApplyStep",step); Settle(view); }
        Assert(original.SequenceEqual(Find<Canvas>(view,"TokenCanvas").Children.OfType<Border>()),"Infix recreated tokens");
        Contained(Find<Canvas>(view,"TokenCanvas"));
        Click(view,"Start_Click"); Find<TextBox>(view,"InfixExpressionInput").Text="2+2"; Click(view,"Build_Click");
        Assert(Field(view,"timer") is null,"Build left old timer active");
        Find<TextBox>(view,"InfixExpressionInput").Text="4*5"; Click(view,"Start_Click");
        Assert(((InfixConversionResult)Field(view,"conversion")!).Postfix=="4 5 *","Start used stale expression");
        Click(view,"Reset_Click"); Assert(Find<Canvas>(view,"TokenCanvas").Children.Count==0,"Infix reset");
        Console.WriteLine("PASS infix: stable token identity, bounds, rebuild cancellation, edited input, reset");
    }

    private static void TestTree()
    {
        var view=new ExpressionTreeView(); Show(view);
        Find<TextBox>(view,"ExpressionInput").Text=string.Join("+",Enumerable.Repeat("1",60));
        Click(view,"BuildTree_Click"); Contained(Find<Canvas>(view,"TreeCanvas"));
        Assert(Find<Canvas>(view,"TreeCanvas").Height>6000,"Deep tree is clipped");
        Find<TextBox>(view,"ExpressionInput").Text="(2+3)*(2+3)";
        Click(view,"StartTree_Click");
        var cache=(LruExpressionCache)Field(view,"_lruCache")!;
        Assert(cache.Count==0,"Future cache leaked before first animation step");
        var steps=(IReadOnlyList<EvaluationStep>)Field(view,"steps")!;
        for(var i=0;i<steps.Count;i++) Call(view,"ApplyStep",i);
        Assert(Find<TextBlock>(view,"ResultText").Text=="25","Animated tree result");
        Assert(cache.Count==2,"Playback cache state");
        Click(view,"ResetTree_Click"); Assert(cache.Count==0,"Cache reset");
        Console.WriteLine("PASS tree: 60-level geometry, edited input, stepwise cache, result, reset");
    }

    private static void TestBank()
    {
        var view=new QueueView(); Show(view);
        Find<TextBox>(view,"ClientInput").Text="0 2 A\n0 1 B\n0 3 C";
        Find<TextBox>(view,"ClientInput").Text="A, 0, 1\nB, 0, 1\nC, 0, 1";
        Click(view,"StartShift_Click"); Click(view,"StopShift_Click");
        for(var i=0;i<3;i++) Call(view,"AdvanceOneMinute");
        Assert((int)Field(view,"servedCount")! == 3 && (int)Field(view,"totalWaitMinutes")! == 3,"Bank service boundary / FIFO wait times");
        // Use the documented formatter, avoiding dependence on human input syntax.
        var customers=BankService.GenerateRandom(new Random(42),480);
        Find<TextBox>(view,"ClientInput").Text=BankService.ToInput(customers);
        Click(view,"StartShift_Click"); Click(view,"StopShift_Click");
        for(var i=0;i<480;i++) { Call(view,"AdvanceOneMinute"); Call(view,"AnimateCustomers"); Settle(view); }
        Assert((int)Field(view,"busyMinutes")!+(int)Field(view,"idleMinutes")! == 480,"Bank accounting");
        Click(view,"GenerateClients_Click"); Pump(1700);
        Assert(((IList)Field(view,"customerVisuals")!).Count==0,"Delayed cleanup corrupted bank reset");
        Console.WriteLine("PASS bank: full shift accounting, reset with pending cleanup");
    }

    private static void TestViewport()
    {
        var canvas=new Canvas { Width=12000,Height=6000 };
        var viewport=new SceneViewport { Scene=canvas }; Show(viewport); Pump();
        var scale=(Avalonia.Media.ScaleTransform)canvas.RenderTransform!;
        Assert(scale.ScaleX<0.1 && scale.ScaleX>0,"Fit does not include complete scene");
        host.Width=700;host.Height=400;Pump();
        var after=(Avalonia.Media.ScaleTransform)canvas.RenderTransform!;
        Assert(after.ScaleX<scale.ScaleX,"Fit does not follow resize");
        Call(viewport,"Zoom",1.0); Pump();
        Assert(((Avalonia.Media.ScaleTransform)canvas.RenderTransform!).ScaleX==1,"Manual zoom");
        host.Width=900; Pump();
        Assert(((Avalonia.Media.ScaleTransform)canvas.RenderTransform!).ScaleX==1,"Resize discarded manual zoom");
        Console.WriteLine("PASS viewport: large scene fit, resize");
    }
}
