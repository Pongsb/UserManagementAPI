using UserManagementAPI.Models;

namespace UserManagementAPI.Services;

public enum StoreResult { Success, NotFound, DuplicateEmail }

public sealed class UserStore
{
    private readonly object _gate = new();
    private readonly Dictionary<int, User> _users = new();
    private readonly Dictionary<string, int> _emailIds = new(StringComparer.OrdinalIgnoreCase);
    private int _nextId = 1;

    public User[] GetAll()
    {
        lock (_gate)
        {
            // Return a snapshot, never the live dictionary or mutable user objects.
            return _users.Values.OrderBy(user => user.Id).ToArray();
        }
    }

    public User? GetById(int id)
    {
        lock (_gate)
        {
            return _users.GetValueOrDefault(id);
        }
    }

    public (StoreResult Result, User? User) Create(string name, string email)
    {
        name = name.Trim();
        email = email.Trim();
        lock (_gate)
        {
            if (_emailIds.ContainsKey(email))
                return (StoreResult.DuplicateEmail, null);

            // One lock makes uniqueness checking, ID assignment and insertion atomic.
            var user = new User(_nextId++, name, email);
            _users.Add(user.Id, user);
            _emailIds.Add(email, user.Id);
            return (StoreResult.Success, user);
        }
    }

    public (StoreResult Result, User? User) Update(int id, string name, string email)
    {
        name = name.Trim();
        email = email.Trim();
        lock (_gate)
        {
            if (!_users.TryGetValue(id, out var existing))
                return (StoreResult.NotFound, null);

            if (_emailIds.TryGetValue(email, out var ownerId) && ownerId != id)
                return (StoreResult.DuplicateEmail, null);

            var updated = new User(id, name, email);
            _emailIds.Remove(existing.Email);
            _emailIds[email] = id;
            _users[id] = updated;
            return (StoreResult.Success, updated);
        }
    }

    public bool Delete(int id)
    {
        lock (_gate)
        {
            if (!_users.Remove(id, out var user))
                return false;

            _emailIds.Remove(user.Email);
            return true;
        }
    }
}
