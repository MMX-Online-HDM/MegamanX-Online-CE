using System;

namespace MMXOnline;

public static class ArrayExtensions {
	/// <summary>Searches for an element that matches the conditions defined by the specified predicate, and returns the zero-based index of the first occurrence within the entire <see cref="T:System.Array" />.</summary>
	/// <param name="array">The one-dimensional, zero-based <see cref="T:System.Array" /> to search.</param>
	/// <param name="match">The <see cref="T:System.Predicate`1" /> that defines the conditions of the element to search for.</param>
	/// <typeparam name="T">The type of the elements of the array.</typeparam>
	/// <exception cref="T:System.ArgumentNullException">
	/// <paramref name="array" /> is <see langword="null" />.
	/// -or-
	/// <paramref name="match" /> is <see langword="null" />.</exception>
	/// <returns>The zero-based index of the first occurrence of an element that matches the conditions defined by <paramref name="match" />, if found; otherwise, -1.</returns>
	public static int FindIndex<T>(this T[] array, Predicate<T> match) {
		return Array.FindIndex(array, match);
	}

	public static int IndexOf(this Array array, object? value) {
		return Array.IndexOf(array, value);
	}
}
