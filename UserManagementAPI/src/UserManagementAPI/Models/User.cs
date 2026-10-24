namespace UserManagementAPI.Models;

// Immutable records prevent callers from modifying data outside the store's lock.
public sealed record User(int Id, string Name, string Email);
