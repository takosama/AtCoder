using Rin;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using   System.Runtime.Intrinsics.X86;
using System.Runtime.CompilerServices;
using Rin.rMos;



namespace Rin.rMos
{
    interface IMosFunctions<T, U>
    {
        public U ComputeFoldL_L(Span<T> arr, U result);
        public U ComputeFoldL_R(Span<T> arr, U result);
        public U ComputeFoldR_L(Span<T> arr, U result);
        public U ComputeFoldR_R(Span<T> arr, U result);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T">arr</typeparam>
    /// <typeparam name="U">result</typeparam>
    unsafe class Mos<T, U>
        where T : unmanaged
        where U : unmanaged
    {
        T[] _arr;
        int _r;
        int _l;
        U result;
        bool IsFirst = true;
        List<(int id, int l, int r)>[] _mosquerys;
        int _QuerysLength = 0;
        IMosFunctions<T, U> Function;
        public Mos(T[] arr, (int id, int l, int r)[] querys, IMosFunctions<T, U> functions)
        {
            this._arr = arr;

            querys = querys.OrderBy(x => x.l).ToArray();
            _QuerysLength = querys.Length;

            int size = 1 + (int)Math.Sqrt(querys.Length);

            _mosquerys = new List<(int id, int l, int r)>[size + 1];
            for (int i = 0; i < _mosquerys.Length; i++)
                _mosquerys[i] = new List<(int id, int l, int r)>();
            for (int i = 0; i < querys.Length; i++)
                _mosquerys[Math.Min(size, querys[i].l / size)].Add(querys[i]);

            for (int i = 0; i < _mosquerys.Length; i++)
                if (i % 2 == 0)
                    _mosquerys[i] = _mosquerys[i].OrderBy(x => x.r).ToList();
                else
                    _mosquerys[i] = _mosquerys[i].OrderByDescending(x => x.r).ToList();

            Function = functions;
        }

        public U[] RunFold()
        {
            var rtn = new U[_QuerysLength];
            var span = this._arr.AsSpan();
            foreach (var qs in _mosquerys)
                foreach (var q in qs)
                {
                    SetLRFold(q.l, q.r, span);
                    rtn[q.id] = this.result;
                }
            return rtn;
        }

        void SetLRFold(int l, int r, Span<T> arr)
        {
            if (IsFirst)
            {
                _l = l;
                _r = l - 1;
                SetRBitFold(r, arr);
                _r = r;
                IsFirst = false;
            }
            else
            {
                SetRBitFold(r, arr);
                SetLBitFold(l, arr);

                _l = l;
                _r = r;
            }
        }


        void SetLBitFold(int l, Span<T> arr)
        {
            if (l > _l)
                result = Function.ComputeFoldL_R(arr.Slice(_l, l - _l), result);
            else
                result = Function.ComputeFoldL_L(arr.Slice(l, _l - l), result);
        }
        void SetRBitFold(int r, Span<T> arr)
        {
            if (r > _r)
                result = Function.ComputeFoldR_R(arr.Slice(_r + 1, r - _r), result);
            else
                result = Function.ComputeFoldR_L(arr.Slice(r + 1, _r - r), result);
        }

    }
}

namespace Rin
{
    public static class ObjectExtension
    {
        public static T DeepClone<T>(this T src)
        {
            using (var memoryStream = new System.IO.MemoryStream())
            {
                var binaryFormatter
                  = new System.Runtime.Serialization
                        .Formatters.Binary.BinaryFormatter();
                binaryFormatter.Serialize(memoryStream, src); // シリアライズ
                memoryStream.Seek(0, System.IO.SeekOrigin.Begin);
                return (T)binaryFormatter.Deserialize(memoryStream); // デシリアライズ
            }
        }
    }
    public class BITree
    {
        int[] _arr;
        int lng = 0;
        public BITree(int[] arr)
        {
            this._arr = new int[arr.Length];
            this.lng = arr.Length;

            for (int i = 0; i < arr.Length; i++)
            {
                var lsb = GetBitLSB(i);

                GetNums(this._arr, arr[i], i);
            }
        }

        void GetNums(int[] arr, int w, int n)
        {
            n++;

            while (n <= arr.Length)
            {
                Console.Write(n - 1 + " ");
                arr[n - 1] += w;
                int r = n & -n;
                n += r;
            }
            Console.WriteLine();
        }

        int GetBitLSB(int n)
        {
            n |= (n << 1);
            n |= (n << 2);
            n |= (n << 4);
            n |= (n << 8);
            n |= (n << 16);
            return 32 - GetBitCount(n);
        }

        int GetBitCount(int n)
        {
            var count = (n & 0x55555555) + ((n >> 1) & 0x55555555);
            count = (count & 0x33333333) + ((count >> 2) & 0x33333333);
            count = (count & 0x0f0f0f0f) + ((count >> 4) & 0x0f0f0f0f);
            count = (count & 0x00ff00ff) + ((count >> 8) & 0x00ff00ff);
            return (count & 0x0000ffff) + ((count >> 16) & 0x0000ffff);
        }
    }
    public class MyArrayIter<T>
    {
        public int End
        {
            get; private set;
        }

        public int Start
        {
            get; private set;
        }
        T[] _arr;

        public int Index { get; set; }
        public MyArrayIter(T[] arr, int index = 0)
        {
            this._arr = arr;
            this.Start = 0;
            this.End = arr.Length;
            this.Index = index;
        }
        public MyArrayIter(T[] arr, int start, int end, int index = 0)
        {
            this._arr = arr;
            if (start > end) throw new Exception();
            this.Start = start;
            this.End = end - start;
            this.Index = index;
        }

        public T Value
        {
            get
            {
                return this[Index];
            }
            set
            {
                this[Index] = value;
            }
        }

        public T this[int index]
        {
            get
            {
                if (GetIndex(index, out var i))
                    return _arr[i];
                else
                    throw new IndexOutOfRangeException();
            }
            set
            {
                if (GetIndex(index, out var i))
                    _arr[i] = value;
                else
                    throw new IndexOutOfRangeException();
            }
        }

        bool GetIndex(int n, out int index)
        {
            index = n + Start;
            if (index < Start || n >= this.End)
                return false;
            return true;
        }
        public static implicit operator MyArrayIter<T>(T[] t)
        {
            return new MyArrayIter<T>(t);
        }


        public static bool operator true(MyArrayIter<T> t)
        {
            return t.GetIndex(t.Index, out var _);
        }
        public static bool operator false(MyArrayIter<T> t)
        {
            return !t.GetIndex(t.Index, out var _);
        }
        public static bool operator <(MyArrayIter<T> t, int num)
        {
            return t.Index + t.Start < num;
        }
        public static bool operator >(MyArrayIter<T> t, int num)
        {
            return t.Index + t.Start > num;
        }

        public static MyArrayIter<T> operator +(MyArrayIter<T> t, int num)
        {
            t.Index += num;
            return t;
        }
        public static MyArrayIter<T> operator -(MyArrayIter<T> t, int num)
        {
            t.Index -= num;
            return t;
        }
        public static MyArrayIter<T> operator ++(MyArrayIter<T> t)
        {
            t.Index++;
            return t;
        }
        public static MyArrayIter<T> operator --(MyArrayIter<T> t)
        {
            t.Index--;
            return t;
        }
    }
    public static class ArrayEx
    {
        /// <summary>
        /// ソート済み配列の中身を比較してvalue以上になる最小のインデックスを返します
        /// もし配列の最大値よりもvalueが大きかったらindex外の値を返します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MyArrayIter<T> GetLower<T>(this T[] arr, T value) where T : IComparable
        {
            var l = 0;
            var r = arr.Length - 1;
            while (l <= r)
            {
                var mid = l + (r - l) / 2;
                var res = arr[mid].CompareTo(value);
                if (res == -1) l = mid + 1;
                else r = mid - 1;
            }
            return new MyArrayIter<T>(arr, l);
        }

        /// <summary>
        /// ソート済み配列の中身を比較してvalueより大きくなる最小のインデックスを返します
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arr"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static MyArrayIter<T> GetUpper<T>(this T[] arr, T value) where T : IComparable
        {
            var l = 0;
            var r = arr.Length - 1;
            while (l <= r)
            {
                var mid = l + (r - l) / 2;
                var res = arr[mid].CompareTo(value);
                if (res <= 0) l = mid + 1;
                else r = mid - 1;
            }
            return new MyArrayIter<T>(arr, l);
        }
    }
    public unsafe class MyArray<T>: IDisposable
        where T : unmanaged
    {
        private bool disposedValue;
        int lng = 0;
       public T* ptr ;
        IntPtr iptr;
        public MyArray(int size,bool IsZeroClear=true)
        {
            lng = size;
            iptr = Marshal.AllocCoTaskMem(size * sizeof(T));
            ptr = (T*)iptr.ToPointer();
            if(IsZeroClear)
            for(int i=0;i<lng;i++)
                ptr[i] = default(T); 
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Marshal.FreeCoTaskMem(iptr);
                disposedValue = true;
            }
        }
        ~MyArray()
        {
            Dispose(disposing: false);
        }
        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public unsafe class MyBitArray : IDisposable
    {
        IntPtr iptr;
  public      int* ptr;
        private bool disposedValue;
        int lng = 0;

        public MyBitArray(int n)
        {
            lng = (int)Math.Ceiling(1.0 * n / 32);
            iptr = Marshal.AllocCoTaskMem(lng * 4);
            ptr = (int*)iptr.ToPointer();
            for (int i = 0; i < lng; i++)
                ptr[i] = 0;
        }
        public int GetValue(ref int index)
        {
            return (ptr[index / 32] & 1 << (index % 32));
        }
        public void FlipValue(ref int index)
        {
            ptr[index / 32] ^= (1 << (index % 32));
        }

        public int FlipAndGetFliped(int index)
        {
            var p = ptr + Math.DivRem(index, 32, out var bit);
            var shift = 1 <<bit;
            *p ^= shift;
            return *p & shift;
        }
        public void SetValue(ref int index, bool b)
        {
            if (!b)
                ptr[index / 32] &= ~(1 << (index % 32));
            else
                ptr[index / 32] |= (1 << (index % 32));
        }
        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Marshal.FreeCoTaskMem(iptr);
                disposedValue = true;
            }
        }
        ~MyBitArray()
        {
            Dispose(disposing: false);
        }
        void IDisposable.Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }

    public class Scanner
    {
        public static string ReadLine()
        {
            return Console.ReadLine();
        }
        public static int ReadInt()
        {
            return int.Parse(Console.ReadLine());
        }

        public static int[] ReadIntArray()
        {
            return Console.ReadLine().Split(' ').Select(x => int.Parse(x)).ToArray();
        }

        public static int[][] ReadIntArrayMulti(int count)
        {
            var rtn = new int[count][];
            for (int i = 0; i < rtn.Length; i++)
                rtn[i] = Console.ReadLine().Split(' ').Select(x => int.Parse(x)).ToArray();
            return rtn;
        }
    }

}
namespace atcoder
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            abc242p.c();
        }
    }


    class abc242p
    {
        public static void c()
        {
            const int mod = 998244353;
            var num = Scanner.ReadInt();
            int[][] arr = new int[num][];

            for (int i = 0; i < arr.Length; i++)
                arr[i] = new int[9];

            for (int i = 0; i < 9; i++)
                arr[0][i] = 1;

            for (int i = 1; i < num; i++)
                for (int j = 0; j < 9; j++)
                {
                    if (j == 0)
                        arr[i][j] = (arr[i - 1][j + 1] + arr[i - 1][j])%mod;
                    else if (j == 8)
                        arr[i][j] =( arr[i - 1][j - 1] + arr[i - 1][j])%mod;
                    else
                        arr[i][j] = ((arr[i - 1][j - 1] + arr[i - 1][j])%mod + arr[i - 1][j+1])%mod;
                }

            int sum = 0;
            for (int i = 0; i < 9; i++)
                sum =(sum+ arr[num - 1][i])%mod;

            Console.WriteLine(sum);
        }
    }
}
