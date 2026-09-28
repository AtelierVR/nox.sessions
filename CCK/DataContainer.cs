using System;
using System.Collections.Generic;
using Nox.CCK.Events;
using Nox.Entities;

namespace Nox.CCK.Sessions {
	/// <summary>
	/// Default in-memory implementation of <see cref="IDataContainer"/>.
	/// Values are kept as-is (any CLR object), never serialized or persisted.
	/// </summary>
	public class DataContainer : IDataContainer {
		private readonly Dictionary<string, object> _values = new();

		public NoxEvent<string[], object, object> OnChanged { get; } = new();

		public T Get<T>(string[] key, T @default = default) {			
			if (key == null || key.Length == 0)
				return @default;

			return _values.TryGetValue(Join(key), out var value) && value is T typed
				? typed
				: @default;
		}

		public T Get<T>(string key, T @default = default)
			=> Get(key?.Split('.') ?? Array.Empty<string>(), @default);

		public bool Has(string[] key)
			=> key is { Length: > 0 } && _values.ContainsKey(Join(key));

		public bool Has(string key)
			=> Has(key?.Split('.'));

		public void Set<T>(string[] key, T value) {
			if (key == null || key.Length == 0)
				return;

			var name = Join(key);
			_values.TryGetValue(name, out var old);
			_values[name] = value;
			OnChanged.Invoke(key, value, old);
		}

		public void Set<T>(string key, T value)
			=> Set(key.Split('.'), value);

		public void Delete(string[] key) {
			if (key == null || key.Length == 0)
				return;

			var name = Join(key);
			_values.TryGetValue(name, out var old);
			_values.Remove(name);
			OnChanged.Invoke(key, null, old);
		}

		public void Delete(string key)
			=> Delete(key.Split('.'));

		/// <summary>Removes every value without raising <see cref="OnChanged"/>.</summary>
		public void Clear()
			=> _values.Clear();

		/// <inheritdoc />
		public void Dispose()
			=> Clear();

		private static string Join(string[] key)
			=> string.Join(".", key);
	}
}
