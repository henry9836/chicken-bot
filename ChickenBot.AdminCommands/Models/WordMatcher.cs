namespace ChickenBot.AdminCommands.Models
{
	public struct WordMatcher
	{
		/// <summary>
		/// The word being scanned
		/// </summary>
		public string Word { get; }

		/// <summary>
		/// The character ignore checker, allowing for certain characters to be skipped instead of breaking a match
		/// </summary>
		public IgnoreCharacters Ignore { get; }

		/// <summary>
		/// The <seealso cref="char"/> comparer, to check if 2 characters are considered equal
		/// </summary>
		public IEqualityComparer<char> CharComparer { get; set; } = new InvariantCharComparer();

		public WordMatcher(string word, IgnoreCharacters ignore)
		{
			Word = word;
			Ignore = ignore;
		}

		/// <summary>
		/// Runs a forward scan across a string looking for matches against a certain string.
		/// </summary>
		/// <param name="text">The text to scan</param>
		/// <returns><see langword="true"/> if <seealso cref="Word"/> was detected in <paramref name="text"/></returns>
		public bool Matches(string text)
		{
			// the current match index of Word, or 0 if a a match is a potential match hasn't been found
			var index = 0;
			// the index the last potential match started
			var matchStarted = -1;

			for (int i = 0; i < Word.Length; i++)
			{
				if (index >= Word.Length)
				{
					return true;
				}

				var cha = text[i];

				if (CharComparer.Equals(Word[index], cha))
				{
					index++;
					if (matchStarted == -1)
					{
						Ignore.Reset();
						matchStarted = i;
					}
					continue;
				}
				else if (matchStarted != -1 && Ignore.ShouldIgnore(cha))
				{
					continue;
				}

				// Does not match
				if (matchStarted != -1)
				{
					// Was in match, back track
					i = matchStarted + 1;
				}
				matchStarted = -1;
				index = 0;

			}

			return index >= Word.Length;
		}
	}
}
