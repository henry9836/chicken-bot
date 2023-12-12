using System.Text.RegularExpressions;
using ChickenBot.API;

namespace ChickenBot.Music.TrackResolve.Models
{
	/// <summary>
	/// Marks a method as a track resolver
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public class TrackResolverAttribute : Attribute
	{
		/// <summary>
		/// The host/domain name this resolver is valid for, or <see langword="null"/> if it is a general query resolver
		/// </summary>
		public Regex[] Hosts { get; set; }

		/// <summary>
		/// Marks a method as a URI resolver for the specified host/domain names
		/// </summary>
		/// <param name="hosts">The hosts/domains this resolver is valid for</param>
		public TrackResolverAttribute(params string[] hosts)
		{
			Hosts = hosts.Select(host =>
			{
				return new Regex(host.CreateRegexExpression(true), RegexOptions.IgnoreCase);
			}).ToArray();
		}
	}
}
