using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ChickenBot.API.Atrributes
{
	public class RequireAnyAttribute : CheckBaseAttribute
	{
		private readonly CheckBaseAttribute[] m_Attributes;

		public RequireAnyAttribute(params CheckBaseAttribute[] attributes)
		{
			m_Attributes = attributes;
		}

		public RequireAnyAttribute(CheckBaseAttribute[] provided, params CheckBaseAttribute[] attributes)
		{
			m_Attributes = provided.Concat(attributes).ToArray();
		}

		public override async Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			foreach(var check in m_Attributes)
			{
				if (await check.ExecuteCheckAsync(ctx, help))
				{
					return true;
				}
			}
			return false;
		}
	}

	public class RequireAnyAttribute<A> : RequireAnyAttribute 
		where A : CheckBaseAttribute, new()
	{
		public RequireAnyAttribute(params CheckBaseAttribute[] attributes)
			: base(attributes, new A()) { }
	}

	public class RequireAnyAttribute<A, B> : RequireAnyAttribute 
		where A : CheckBaseAttribute, new() 
		where B : CheckBaseAttribute, new()
	{
		public RequireAnyAttribute(params CheckBaseAttribute[] attributes)
			: base(attributes, new A(), new B()) { }
	}

	public class RequireAnyAttribute<A, B, C> : RequireAnyAttribute
		where A : CheckBaseAttribute, new()
		where B : CheckBaseAttribute, new()
		where C : CheckBaseAttribute, new()
	{
		public RequireAnyAttribute(params CheckBaseAttribute[] attributes)
			: base(attributes, new A(), new B(), new C()) { }
	}

	public class RequireAnyAttribute<A, B, C, D> : RequireAnyAttribute
		where A : CheckBaseAttribute, new()
		where B : CheckBaseAttribute, new()
		where C : CheckBaseAttribute, new()
		where D : CheckBaseAttribute, new()
	{
		public RequireAnyAttribute(params CheckBaseAttribute[] attributes)
			: base(attributes, new A(), new B(), new C(), new D()) { }
	}

	public class RequireAnyAttribute<A, B, C, D, E> : RequireAnyAttribute
		where A : CheckBaseAttribute, new()
		where B : CheckBaseAttribute, new()
		where C : CheckBaseAttribute, new()
		where D : CheckBaseAttribute, new()
		where E : CheckBaseAttribute, new()
	{
		public RequireAnyAttribute(params CheckBaseAttribute[] attributes)
			: base(attributes, new A(), new B(), new C(), new D(), new E())
		{ }
	}
	public class RequireAnyAttribute<A, B, C, D, E, F> : RequireAnyAttribute
		where A : CheckBaseAttribute, new()
		where B : CheckBaseAttribute, new()
		where C : CheckBaseAttribute, new()
		where D : CheckBaseAttribute, new()
		where E : CheckBaseAttribute, new()
		where F : CheckBaseAttribute, new()
	{
		public RequireAnyAttribute(params CheckBaseAttribute[] attributes)
			: base(attributes, new A(), new B(), new C(), new D(), new E())
		{ }
	}
}
