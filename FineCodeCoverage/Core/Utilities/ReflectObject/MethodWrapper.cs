using System;
using System.Collections.Generic;
using System.Reflection;

namespace FineCodeCoverage.Core.Utilities
{
    internal class MethodWrapper
	{
		private readonly MethodInfo wrapped;
		private readonly object owner;

		private static Type ThisType = typeof(MethodWrapper);

		private static List<MethodInfo> WrapFuncs = new List<MethodInfo>
		{
			ThisType.GetMethod(nameof(WrapFunc)),
			ThisType.GetMethod(nameof(WrapFunc1)),
			ThisType.GetMethod(nameof(WrapFunc2)),
			ThisType.GetMethod(nameof(WrapFunc3)),
			ThisType.GetMethod(nameof(WrapFunc4)),
			ThisType.GetMethod(nameof(WrapFunc5)),
			ThisType.GetMethod(nameof(WrapFunc6)),
			ThisType.GetMethod(nameof(WrapFunc7)),
			ThisType.GetMethod(nameof(WrapFunc8)),

		};

		private static List<MethodInfo> ActionFuncs = new List<MethodInfo>
		{
			ThisType.GetMethod(nameof(WrapAction)),
			ThisType.GetMethod(nameof(WrapAction1)),
			ThisType.GetMethod(nameof(WrapAction2)),
			ThisType.GetMethod(nameof(WrapAction3)),
			ThisType.GetMethod(nameof(WrapAction4)),
			ThisType.GetMethod(nameof(WrapAction5)),
			ThisType.GetMethod(nameof(WrapAction6)),
			ThisType.GetMethod(nameof(WrapAction7)),
			ThisType.GetMethod(nameof(WrapAction8)),

		};

		public static Delegate CreateDelegateWrapper(MethodInfo wrappedMethod, object owner, Type funcOrActionType, bool isAction)
		{
			var methodWrapper = new MethodWrapper(wrappedMethod, owner);
			var genericArguments = funcOrActionType.GetGenericArguments();
			MethodInfo wrapMethod = null;
			if (isAction)
			{
				if (genericArguments.Length == 0)
				{
					wrapMethod = ActionFuncs[0];
				}
				else
				{
					wrapMethod = ActionFuncs[genericArguments.Length].MakeGenericMethod(genericArguments);
				}
			}
			else
			{
				wrapMethod = WrapFuncs[genericArguments.Length - 1].MakeGenericMethod(genericArguments);
			}
			return Delegate.CreateDelegate(funcOrActionType, methodWrapper, wrapMethod);

		}

		public MethodWrapper(MethodInfo wrapped, object owner)
		{
			this.wrapped = wrapped;
			this.owner = owner;
		}
		#region funcs
		public TReturn WrapFunc<TReturn>()
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { });
		}

		public TReturn WrapFunc1<T1, TReturn>(T1 t1)
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { t1 });
		}

		public TReturn WrapFunc2<T1, T2, TReturn>(T1 t1, T2 t2)
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { t1, t2 });
		}

		public TReturn WrapFunc3<T1, T2, T3, TReturn>(T1 t1, T2 t2, T3 t3)
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { t1, t2, t3 });
		}

		public TReturn WrapFunc4<T1, T2, T3, T4, TReturn>(T1 t1, T2 t2, T3 t3, T4 t4)
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { t1, t2, t3, t4 });
		}

		public TReturn WrapFunc5<T1, T2, T3, T4, T5, TReturn>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { t1, t2, t3, t4, t5 });
		}

		public TReturn WrapFunc6<T1, T2, T3, T4, T5, T6, TReturn>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { t1, t2, t3, t4, t5, t6 });
		}

		public TReturn WrapFunc7<T1, T2, T3, T4, T5, T6, T7, TReturn>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { t1, t2, t3, t4, t5, t6, t7 });
		}

		public TReturn WrapFunc8<T1, T2, T3, T4, T5, T6, T7, T8, TReturn>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
		{
			return (TReturn)wrapped.Invoke(owner, new object[] { t1, t2, t3, t4, t5, t6, t7, t8 });
		}
		#endregion

		#region actions
		public void WrapAction()
		{
			wrapped.Invoke(owner, new object[] { });
		}
		public void WrapAction1<T1>(T1 t1)
		{
			wrapped.Invoke(owner, new object[] { t1 });
		}

		public void WrapAction2<T1, T2>(T1 t1, T2 t2)
		{
			wrapped.Invoke(owner, new object[] { t1, t2 });
		}

		public void WrapAction3<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
		{
			wrapped.Invoke(owner, new object[] { t1, t2, t3 });
		}

		public void WrapAction4<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
		{
			wrapped.Invoke(owner, new object[] { t1, t2, t3, t4 });
		}

		public void WrapAction5<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
		{
			wrapped.Invoke(owner, new object[] { t1, t2, t3, t4, t5 });
		}

		public void WrapAction6<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
		{
			wrapped.Invoke(owner, new object[] { t1, t2, t3, t4, t5, t6 });
		}

		public void WrapAction7<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
		{
			wrapped.Invoke(owner, new object[] { t1, t2, t3, t4, t5, t6, t7 });
		}

		public void WrapAction8<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
		{
			wrapped.Invoke(owner, new object[] { t1, t2, t3, t4, t5, t6, t7, t8 });
		}



		#endregion
	}

}
