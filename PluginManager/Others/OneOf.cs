using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others
{
    public class OneOf<T0, T1>
    {
        public T0 Item0 { get; }
        public T1 Item1 { get; }

        public object? Value => Item0 != null ? Item0 : Item1;

        public OneOf(T0 item0)
        {
            Item0 = item0;
        }

        public OneOf(T1 item1)
        {
            Item1 = item1;
        }

        public static implicit operator OneOf<T0, T1>(T0 item0) => new OneOf<T0, T1>(item0);
        public static implicit operator OneOf<T0, T1>(T1 item1) => new OneOf<T0, T1>(item1);

        public void Match(Action<T0> item0, Action<T1> item1)
        {
            if (Item0 != null)
                item0(Item0);
            else
                item1(Item1);
        }

        public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1)
        {
            return Item0 != null ? item0(Item0) : item1(Item1);
        }

        public Type GetActualType()
        {
            return Item0 != null ? Item0.GetType() : Item1.GetType();
        }
    }

    public class OneOf<T0, T1, T2>
    {
        public T0 Item0 { get; }
        public T1 Item1 { get; }
        public T2 Item2 { get; }

        public OneOf(T0 item0)
        {
            Item0 = item0;
        }

        public OneOf(T1 item1)
        {
            Item1 = item1;
        }

        public OneOf(T2 item2)
        {
            Item2 = item2;
        }

        public static implicit operator OneOf<T0, T1, T2>(T0 item0) => new OneOf<T0, T1, T2>(item0);
        public static implicit operator OneOf<T0, T1, T2>(T1 item1) => new OneOf<T0, T1, T2>(item1);
        public static implicit operator OneOf<T0, T1, T2>(T2 item2) => new OneOf<T0, T1, T2>(item2);

        public void Match(Action<T0> item0, Action<T1> item1, Action<T2> item2)
        {
            if (Item0 != null)
                item0(Item0);
            else if (Item1 != null)
                item1(Item1);
            else
                item2(Item2);
        }

        public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1, Func<T2, TResult> item2)
        {
            return Item0 != null ? item0(Item0) : Item1 != null ? item1(Item1) : item2(Item2);
        }
    }

    public class OneOf<T0, T1, T2, T3>
    {
        public T0 Item0 { get; }
        public T1 Item1 { get; }
        public T2 Item2 { get; }
        public T3 Item3 { get; }

        public OneOf(T0 item0)
        {
            Item0 = item0;
        }

        public OneOf(T1 item1)
        {
            Item1 = item1;
        }

        public OneOf(T2 item2)
        {
            Item2 = item2;
        }

        public OneOf(T3 item3)
        {
            Item3 = item3;
        }

        public static implicit operator OneOf<T0, T1, T2, T3>(T0 item0) => new OneOf<T0, T1, T2, T3>(item0);
        public static implicit operator OneOf<T0, T1, T2, T3>(T1 item1) => new OneOf<T0, T1, T2, T3>(item1);
        public static implicit operator OneOf<T0, T1, T2, T3>(T2 item2) => new OneOf<T0, T1, T2, T3>(item2);
        public static implicit operator OneOf<T0, T1, T2, T3>(T3 item3) => new OneOf<T0, T1, T2, T3>(item3);

        public void Match(Action<T0> item0, Action<T1> item1, Action<T2> item2, Action<T3> item3)
        {
            if (Item0 != null)
                item0(Item0);
            else if (Item1 != null)
                item1(Item1);
            else if (Item2 != null)
                item2(Item2);
            else
                item3(Item3);
        }

        public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1, Func<T2, TResult> item2, Func<T3, TResult> item3)
        {
            return Item0 != null ? item0(Item0) : Item1 != null ? item1(Item1) : Item2 != null ? item2(Item2) : item3(Item3);
        }
        
    }
}
