# Unity Extensible Architecture Rules (Low Coupling & High Cohesion)

## 1. OOP Fundamentals and Encapsulation

### Data Encapsulation:
- **Private fields** with controlled access: `private int _health; public int Health { get; private set; }`
- **Collection Protection**: return `IReadOnlyList<T>` instead of direct references to `List<T>`
- **Validation in Setters**: always check input data validity

```csharp
private int _health;
public int Health 
{ 
    get => _health; 
    private set => _health = Mathf.Max(0, value); 
}
```

## 2. Abstractions for Loose Coupling

### Interfaces (primary tool):
- **When**: a contract for behavior of different classes is needed
- **Example**: `IInteractable`, `IDataSerializer`, `IDamageable`

```csharp
public interface IInteractable
{
    void Interact(Player player);
    bool CanInteract { get; }
}
```

### Abstract Classes:
- **When**: there's common code + specific implementations in derived classes
- **Avoid**: calling virtual methods in constructors

## 3. Composition > Inheritance

### Principle:
- Instead of `ElfEnemy : Enemy`, create an `Enemy` with `AttackComponent`, `MovementComponent`
- **Advantage**: flexible behavior combinations, reusability

```csharp
public class Enemy : MonoBehaviour
{
    [SerializeField] private AttackComponent _attackComponent;
    [SerializeField] private MovementComponent _movementComponent;
    [SerializeField] private DefenseComponent _defenseComponent;
}
```

## 4. Events for Notifications (Publisher-Subscriber)

### When to use:
- One system needs to notify multiple others about an event
- Publisher doesn't know the subscribers

```csharp
public class Health : MonoBehaviour
{
    public event System.Action<int> HealthChanged;
    public event System.Action Died;
    
    private int _currentHealth;
    
    public void TakeDamage(int damage)
    {
        _currentHealth -= damage;
        HealthChanged?.Invoke(_currentHealth);
        
        if (_currentHealth <= 0)
            Died?.Invoke();
    }
}
```

### Subscription:
```csharp
// In UI or sound system
healthComponent.HealthChanged += OnHealthChanged;
healthComponent.Died += OnPlayerDied;
```

## 5. Dependency Injection (DI)

### Principle:
- Object receives dependencies externally, doesn't create them itself
- Dependency on interfaces, not concrete classes

### Registration in container:
```csharp
container.Register<IDataSerializer>(() => new JsonDataSerializer());
container.Register<ISaveService>(container => 
    new SaveService(container.Resolve<IDataSerializer>()));
```

### Usage:
```csharp
public class GameManager : MonoBehaviour
{
    private ISaveService _saveService;
    
    public void Initialize(ISaveService saveService)
    {
        _saveService = saveService;
    }
}
```

## 6. Factories for Complex Creation

### When to use:
- Complex object creation logic
- Need to hide creation details

```csharp
public class EnemyFactory
{
    public Enemy CreateEnemy(EnemyType type, Vector3 position)
    {
        var enemy = Object.Instantiate(GetPrefab(type), position, Quaternion.identity);
        SetupComponents(enemy, type);
        return enemy;
    }
}
```

## 7. Data-Oriented Design (Simplified ECS)

### Structure:
- **Entity**: data container (Dictionary<Enum, object>)
- **Components**: pure data
- **Behaviors**: logic operating on data

```csharp
public class Entity : MonoBehaviour
{
    private Dictionary<DataType, object> _data = new();
    
    public T GetData<T>(DataType type) where T : class
    {
        return _data.TryGetValue(type, out var value) ? value as T : null;
    }
}

public class MovementBehavior
{
    public void Update(Entity entity)
    {
        var movementData = entity.GetData<MovementData>(DataType.Movement);
        // movement logic
    }
}
```

## 8. Practical Rules

### Naming:
- **Classes**: nouns, PascalCase (`Enemy`, `PlayerController`)
- **Methods**: verbs, PascalCase (`TakeDamage()`, `CanInteract()`)
- **Events**: past tense (`Died`, `ItemPickedUp`)
- **Subscribers**: `On...` (`OnDied`, `OnItemPickedUp`)

### Project Structure:
```
Assets/
├── Scripts/
│   ├── Core/          (core systems)
│   ├── Gameplay/      (game logic)
│   ├── UI/           (user interface)
│   └── Data/         (ScriptableObjects)
├── Prefabs/
└── Materials/
```

### Configuration:
- **ScriptableObjects** for balance settings
- Separation of data and logic

```csharp
[CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy Data")]
public class EnemyData : ScriptableObject
{
    public int Health;
    public float Speed;
    public int Damage;
}
```

## 9. Anti-Patterns (Avoid)

- **Singletons**: create tight coupling
- **GameObject.Find()**: slow, unreliable
- **Magic numbers**: use named constants
- **God Objects**: classes that do too much
- **Direct references**: prefer interfaces and events

## 10. Quick Checklist

✅ Do I use interfaces instead of concrete classes?  
✅ Is data encapsulated and protected?  
✅ Are events used for notifications?  
✅ Are dependencies injected, not created within the class?  
✅ Is composition preferred over inheritance?  
✅ Is configuration data moved to ScriptableObjects?  
✅ Is the code readable and follows naming conventions?  