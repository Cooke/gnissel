# Gnissel - Micro ORM

An alternative database mapper for .NET, instead of Dapper or Entity Framework.

## Features

- Thread safe database context (can be used as a singleton)
- Async enumerable interface (combines well with System.Async.Linq)
- Utilizes the DataSource API introduced in .NET 7
- No change tracking
- No navigation properties

## Limitations

- Currently only a Postgres adapter exists
- Not battle tested

## Installation

`dotnet add Cooke.Gnissel`

Postgres adapter:
`dotnet add Cooke.Gnissel.Npgsql`

Recommended addition:
`dotnet add System.Async.Linq`

## Setup

Setup adapter (provider specific):

```csharp
var connectionString = "...";
var adapter = new NpgsqlDbAdapter(NpgsqlDataSource.Create(connectionString));
```

Setup DbContext (general)

```csharp
var dbContext = new DbContext(new DbOptions(adapter));
```

## Usage examples

#### Example types

```csharp
public record User(int Id, string Name);
public record Device(int Id, string Name, int UserId);
```

#### Query

```csharp
var allUsers = await dbContext.Query<User>($"SELECT * FROM users").ToArrayAsync();
```

#### Non query

```csharp
await dbContext.Execute($"INSERT INTO users(name) VALUES('Foo')");
```

#### Parameters

```csharp
var userId = 1;
var userById = await dbContext.Query<User>($"SELECT * FROM users WHERE id={userId}").SingleAsync();
```

#### Query multiple types

```csharp
var devicesWithUserName = await dbContext.Query<(string userName, Device device)>(
    $"SELECT u.name, ud.* FROM users AS u JOIN user_devices AS ud ON u.id=ud.user_id")
    .ToArrayAsync();
```

#### Dynamic SQL (injection)

```csharp
var devicesWithUserName = await dbContext.Query<string>(
    $"SELECT name FROM {Sql.Inject("users")}")
    .ToArrayAsync();
```

#### Parameter type mapping

```csharp
// Stored as JSON in database
public record Document(string title, string body);
```

```csharp
var documentTitle = "Development Process";
var documents = await dbContext.Query<Document>(
    $"SELECT document FROM documents WHERE document->'title'={documentTitle:jsonb}")
    .SingleAsync();
```

#### Custom ad-hoc mapping

```csharp
var userIds = await dbContext.Query<int>(
    $"SELECT id FROM users", dbReader => dbReader.GetInt32(0))
    .SingleAsync();
```

#### Transactions

```csharp
await dbContext.Transaction(
    dbContext.Execute($"INSERT INTO users(name) VALUES('foo')"),
    dbContext.Execute($"INSERT INTO users(name) VALUES('bar')"));
```

#### Batching

Currently only non queries are supported.

```csharp
await dbContext.Batch(
    dbContext.Execute($"INSERT INTO users(name) VALUES('foo')"),
    dbContext.Execute($"INSERT INTO users(name) VALUES('bar')"));
```
