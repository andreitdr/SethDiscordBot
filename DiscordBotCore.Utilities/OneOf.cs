namespace DiscordBotCore.Utilities;

 public class OneOf<T0, T1>
    {
        // Which item was set; checking for null would always pick Item0 when it is a value type
        private readonly int _Index;

        public T0 Item0 { get; } = default!;
        public T1 Item1 { get; } = default!;

        public object? Value => _Index == 0 ? Item0 : Item1;

        public OneOf(T0 item0)
        {
            Item0 = item0;
            _Index = 0;
        }

        public OneOf(T1 item1)
        {
            Item1 = item1;
            _Index = 1;
        }

        public static implicit operator OneOf<T0, T1>(T0 item0) => new OneOf<T0, T1>(item0);
        public static implicit operator OneOf<T0, T1>(T1 item1) => new OneOf<T0, T1>(item1);

        public void Match(Action<T0> item0, Action<T1> item1)
        {
            if (_Index == 0)
                item0(Item0);
            else
                item1(Item1);
        }

        public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1)
        {
            return _Index == 0 ? item0(Item0) : item1(Item1);
        }

        public Type GetActualType()
        {
            return _Index == 0 ? Item0?.GetType() ?? typeof(T0) : Item1?.GetType() ?? typeof(T1);
        }
    }

    public class OneOf<T0, T1, T2>
    {
        private readonly int _Index;

        public T0 Item0 { get; } = default!;
        public T1 Item1 { get; } = default!;
        public T2 Item2 { get; } = default!;

        public OneOf(T0 item0)
        {
            Item0 = item0;
            _Index = 0;
        }

        public OneOf(T1 item1)
        {
            Item1 = item1;
            _Index = 1;
        }

        public OneOf(T2 item2)
        {
            Item2 = item2;
            _Index = 2;
        }

        public static implicit operator OneOf<T0, T1, T2>(T0 item0) => new OneOf<T0, T1, T2>(item0);
        public static implicit operator OneOf<T0, T1, T2>(T1 item1) => new OneOf<T0, T1, T2>(item1);
        public static implicit operator OneOf<T0, T1, T2>(T2 item2) => new OneOf<T0, T1, T2>(item2);

        public void Match(Action<T0> item0, Action<T1> item1, Action<T2> item2)
        {
            if (_Index == 0)
                item0(Item0);
            else if (_Index == 1)
                item1(Item1);
            else
                item2(Item2);
        }

        public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1, Func<T2, TResult> item2)
        {
            return _Index == 0 ? item0(Item0) : _Index == 1 ? item1(Item1) : item2(Item2);
        }
    }

    public class OneOf<T0, T1, T2, T3>
    {
        private readonly int _Index;

        public T0 Item0 { get; } = default!;
        public T1 Item1 { get; } = default!;
        public T2 Item2 { get; } = default!;
        public T3 Item3 { get; } = default!;

        public OneOf(T0 item0)
        {
            Item0 = item0;
            _Index = 0;
        }

        public OneOf(T1 item1)
        {
            Item1 = item1;
            _Index = 1;
        }

        public OneOf(T2 item2)
        {
            Item2 = item2;
            _Index = 2;
        }

        public OneOf(T3 item3)
        {
            Item3 = item3;
            _Index = 3;
        }

        public static implicit operator OneOf<T0, T1, T2, T3>(T0 item0) => new OneOf<T0, T1, T2, T3>(item0);
        public static implicit operator OneOf<T0, T1, T2, T3>(T1 item1) => new OneOf<T0, T1, T2, T3>(item1);
        public static implicit operator OneOf<T0, T1, T2, T3>(T2 item2) => new OneOf<T0, T1, T2, T3>(item2);
        public static implicit operator OneOf<T0, T1, T2, T3>(T3 item3) => new OneOf<T0, T1, T2, T3>(item3);

        public void Match(Action<T0> item0, Action<T1> item1, Action<T2> item2, Action<T3> item3)
        {
            if (_Index == 0)
                item0(Item0);
            else if (_Index == 1)
                item1(Item1);
            else if (_Index == 2)
                item2(Item2);
            else
                item3(Item3);
        }

        public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1, Func<T2, TResult> item2, Func<T3, TResult> item3)
        {
            return _Index == 0 ? item0(Item0) : _Index == 1 ? item1(Item1) : _Index == 2 ? item2(Item2) : item3(Item3);
        }
        
    }