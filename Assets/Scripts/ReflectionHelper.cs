using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace KModkit
{
    public static class ReflectionHelper
    {
        /// <summary>
        /// Shortcut for <see cref="BindingFlags"/> to simplify the use of reflections and make it work for any access level
        /// </summary>
        public static readonly BindingFlags AllFlags = BindingFlags.Public
                                             | BindingFlags.NonPublic
                                             | BindingFlags.Instance
                                             | BindingFlags.Static
                                             | BindingFlags.GetField
                                             | BindingFlags.SetField
                                             | BindingFlags.GetProperty
                                             | BindingFlags.SetProperty;

        /// <summary>
        /// The name of the game's internal assembly
        /// </summary>
        public const string GameAssembly = "Assembly-CSharp";

        /// <summary>
        /// Get the specified type
        /// </summary>
        /// <param name="fullName">Name of the type</param>
        /// <param name="assemblyName">Name of the assembly containing the type (optional)</param>
        /// <returns>The found type</returns>
        public static Type FindType(string fullName, string assemblyName = null)
        {
	        return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetSafeTypes()).FirstOrDefault(t => t.FullName != null && t.FullName.Equals(fullName) && (assemblyName==null || t.Assembly.GetName().Name.Equals(assemblyName)));
        }
        
        /// <summary>
        /// Gets the specified type from the game's internal code
        /// </summary>
        /// <param name="fullName">Name of the type</param>
        /// <returns></returns>
        public static Type FindGameType(string fullName)
        {
            return FindType(fullName, GameAssembly);
        }
    
        /// <summary>
        /// Get all successfully loaded types in the passed Assembly
        /// </summary>
        /// <param name="assembly">Assembly to search types in</param>
        /// <returns>The types in the assembly</returns>
        public static IEnumerable<Type> GetSafeTypes(this Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(x => x != null);
            }
            catch (Exception)
            {
                return new List<Type>();
            }
        }
        
        static readonly Dictionary<string, MemberInfo> MemberCache = new Dictionary<string, MemberInfo>();
        	public static T GetCachedMember<T>(this Type type, string member) where T : MemberInfo
        	{
        		// Use AssemblyQualifiedName and the member name as a unique key to prevent a collision if two types have the same member name
        		var key = type.AssemblyQualifiedName + member;
        #pragma warning disable IDE0038
        		if (MemberCache.ContainsKey(key)) return MemberCache[key] is T ? (T) MemberCache[key] : null;
        
        		MemberInfo memberInfo = type.GetMember(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance).FirstOrDefault();
        		MemberCache[key] = memberInfo;
        
        		return memberInfo is T ? (T) memberInfo : null;
        #pragma warning restore IDE0038
        	}
        
        	public static T GetValue<T>(this Type type, string member, object target = null)
        	{
        		var fieldMember = type.GetCachedMember<FieldInfo>(member);
        		var propertyMember = type.GetCachedMember<PropertyInfo>(member);
        
        		return (T) ((fieldMember != null ? fieldMember.GetValue(target) : default(T)) ?? (propertyMember != null ? propertyMember.GetValue(target, null) : default(T)));
        	}
        
        	public static void SetValue(this Type type, string member, object value, object target = null)
        	{
        		var fieldMember = type.GetCachedMember<FieldInfo>(member);
        		if (fieldMember != null)
        			fieldMember.SetValue(target, value);
        
        		var propertyMember = type.GetCachedMember<PropertyInfo>(member);
        		if (propertyMember != null)
        			propertyMember.SetValue(target, value, null);
        	}
        
        	public static T CallMethod<T>(this Type type, string method, object target = null, params object[] arguments)
        	{
        		var member = type.GetCachedMember<MethodInfo>(method);
        #pragma warning disable IDE0034, IDE0034WithoutSuggestion, RCS1244
        		return member != null ? (T) member.Invoke(target, arguments) : default(T);
        #pragma warning restore IDE0034, IDE0034WithoutSuggestion, RCS1244
        	}
        
        	public static void CallMethod(this Type type, string method, object target = null, params object[] arguments)
        	{
        		var member = type.GetCachedMember<MethodInfo>(method);
        		if (member != null)
        			member.Invoke(target, arguments);
        	}
        
        	public static T GetValue<T>(this object @object, string member)
        	{
        		return @object.GetType().GetValue<T>(member, @object);
        	}
        
        	public static void SetValue(this object @object, string member, object value)
        	{
        		@object.GetType().SetValue(member, value, @object);
        	}
        
        	public static T CallMethod<T>(this object @object, string member, params object[] arguments)
        	{
        		return @object.GetType().CallMethod<T>(member, @object, arguments);
        	}
        
        	public static void CallMethod(this object @object, string member, params object[] arguments)
        	{
        		@object.GetType().CallMethod(member, @object, arguments);
        	}
    }
}