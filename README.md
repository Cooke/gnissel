# Gnissel - Micro ORM

An alternative database mapper for .NET, instead of Dapper or Entity Framework.

## Features

- Thread safe database context (can be used as a singleton)
- Async enumerable interface (combines well with System.Async.Linq)
- Utilizes the DataSource API introduced in .NET 7
- No change tracking
- No navigation properties
- Strongly typed queries via [Typed namespace](#typed-namespace)

## Limitations

- Currently only a Postgres adapter exists

## Installation

`dotnet add Cooke.Gnissel`

Postgres adapter:
`dotnet add Cooke.Gnissel.Npgsql`

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
var user = await dbContext.QuerySingle<User>($"SELECT * FROM users WHERE id=1");
var maybeUser = await dbContext.QuerySingleOrDefault<User>($"SELECT * FROM users WHERE name=chad LIMIT 1");
```

#### Non query

```csharp
await dbContext.NonQuery($"INSERT INTO users(name) VALUES('Foo')");
```

#### Parameters

```csharp
var userId = 1;
var userById = await dbContext.QuerySingle<User>($"SELECT * FROM users WHERE id={userId}");
```

#### Query multiple types

```csharp
var devicesWithUserName = await dbContext.Query<(string userName, Device device)>(
    $"SELECT u.name, ud.* FROM users AS u JOIN user_devices AS ud ON u.id=ud.user_id")
    .ToArrayAsync();
```

#### Null support

If all columns are null in a complex type, the result will be null for that complex type.

```csharp
var usersWithDevices = await dbContext.Query<(User user, Device? device)>(
    $"SELECT u.*, ud.* FROM users AS u LEFT JOIN user_devices AS ud ON u.id=ud.user_id")
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
    dbContext.NonQuery($"INSERT INTO users(name) VALUES('foo')"),
    dbContext.NonQuery($"INSERT INTO users(name) VALUES('bar')"));
```

#### Batching

Currently only non queries are supported.

```csharp
await dbContext.Batch(
    dbContext.NonQuery($"INSERT INTO users(name) VALUES('foo')"),
    dbContext.NonQuery($"INSERT INTO users(name) VALUES('bar')"));
```

#### Utils

Some utils are provided for easier consumption of the async enumerable result.

##### Group by

```csharp
usersWithDevices // Type: IAsyncEnumerable<(User user, Device device)>
    .GroupBy(
        (u, d) => u, // Group by selector
        (u, device) => device, // Element selector
        (u, devices) => (u, devices.ToArray()), // Result selector
        u => u.id // Group by key selector
    ); 

```

#### Converters (API is experimental)
Converters can be used for custom data mapping of single values. 

Create a subclass of `ConcreteDbConverter<T>` to create a converter for the specific type `T` and override the abstract methods. 

Create a subclass of `ConcreteDbConverterFactory` to create a factory for concrete db converters that can be produces during runtime.

Converters (including converter factories) are enabled either via the DbOptions instace passed into the DbContext constructor or by applying the `DbConverterAttribute` to a type.

##### Optional built in converters
The following converters are included in the library and can be enabled:

* EnumStringDbConverter - converts enum to/from strings  
* NestedValueDbConverter -  converts wrapper objects to the inner wrapped value. Example:
  ```
  [DbConverter(typeof(NestedValueDbConverter))]
  public record DeviceId(string Value);
  ```
  

#### Database migration
```csharp
public class InitialMigration : Migration {
    public override string Id => "Initial migration";

    public override ValueTask Migrate(DbContext db, CancellationToken cancellationToken) =>
         db.NonQuery($"create table users(id integer)");                    
}

var migrations = new List<Migration>()
{
    new InitialMigration(),
    new FuncMigration(
        "drop users table",
        (db, ct) => db.NonQuery($"drop table users")
    )
};
await _db.Migrate(migrations);
```


#### Cancellation support
```csharp
await dbContext.Query<User>($"SELECT * FROM users").ToArrayAsync(cancellationToken);
await dbContext.NonQuery($"INSERT INTO users(name) VALUES('Foo')").ExecuteAsync(cancellationToken);
```


# Typed namespace

The Typed namespace includes support for typed quries.

## Setup

Create a custom DbContext (which may inherit from DbContext but is not required to).

```csharp
public record User(int Id, string Name);
public record Device(int Id, string Name, int UserId);

public class AppDbContext(DbOptions options) : DbContext
{
    public Table<User> Users { get; } = new Table<User>(options);

    public Table<Device> Devices { get; } = new Table<Device>(options);
}
```

```csharp
var dbContext = new AppDbContext(new DbOptions(adapter));
```

## Usage examples

#### Querying

```csharp
var allUsers = await dbContext.Users.ToArrayAsync();
var allBobs = await dbContext.Users.Where(x => x.Name == "Bob").ToArrayAsync();
var allNames = await dbContext.Users.Select(x => x.Name).ToArrayAsync();
var partialDevcies = await dbContext.Devices.Select(x => new { x.Id, DeviceName = x.Name }).ToArrayAsync();
```

#### First or default 

First queries are implemented by using LIMIT 1 in the SQL query.

```csharp
var firstUser = await dbContext.Users.First();
var firstOrNoUser = await dbContext.Users.FirstOrDefault();
var firstBob = await dbContext.Users.First(x => x.Name == "Bob");
var firstOrNoBob = await dbContext.Users.FirstOrDefault(x => x.Name == "Bob");
```

#### Joining

Supported join types are: (inner) join, left join, right join, full (outer) join and cross join.

```csharp
(User, Device)[] bobWithDevices = await dbContext.Users
    .Join(dbContext.Devices, (u, d) => u.Id == d.UserId)
    .Where((u, d) => u.Name == "Bob")
    .ToArrayAsync();
```

#### Insert

```csharp
await dbContext.Users.Insert(new User(0, "Bob"));
await dbContext.Users.Insert(new User(1, "Alice"), new User(2, "Greta"));
```

#### Update

```csharp
await dbContext.Users.Set(x => x.Name, "Robert").Where(x => x.Name == "Bob");
await dbContext.Users.Set(x => x.LastLogin, null).WithoutWhere();
```

#### Delete

```csharp
await dbContext.Users.Delete().Where(x => x.Name == "Bob");
await dbContext.Users.Delete().WithoutWhere();
```

#### Grouping and aggregation

```csharp
var userSummaryByName = await dbContext.Users
    .GroupBy(x => x.Name)
    .Select(x => new { 
        x.Name, 
        Count = Db.Count(),
        MaxAge = Db.Max(x.Age),
        MinAge = Db.Min(x.Age),
        MaxAge = Db.Sum(x.Age),
        AvgAge = Db.Avg(x.Age)
    }).ToArrayAsync();
```

#### Ordering

```csharp
var userSummaryByName = await dbContext.Users
    .OrderBy(x => x.Name)
    .ThenByDesc(x => x.Age)
    .ToArrayAsync();
```