namespace ChickenBot.API.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public abstract class ServiceAliasAttribute : Attribute
    {
        public Type[] AliasTypes { get; }

        public ServiceAliasAttribute(Type[] types)
        {
            AliasTypes = types;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceAliasAttribute<A> : ServiceAliasAttribute
    {
        public ServiceAliasAttribute() : base([typeof(A)]) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceAliasAttribute<A, B> : ServiceAliasAttribute
    {
        public ServiceAliasAttribute() : base([typeof(A), typeof(B)]) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceAliasAttribute<A, B, C> : ServiceAliasAttribute
    {
        public ServiceAliasAttribute() : base([typeof(A), typeof(B), typeof(C)]) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceAliasAttribute<A, B, C, D> : ServiceAliasAttribute
    {
        public ServiceAliasAttribute() : base([typeof(A), typeof(B), typeof(C), typeof(D)]) { }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class ServiceAliasAttribute<A, B, C, D, E> : ServiceAliasAttribute
    {
        public ServiceAliasAttribute() : base([typeof(A), typeof(B), typeof(C), typeof(D), typeof(E)]) { }
    }
}
