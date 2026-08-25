# Sequência 5 — User Authentication Flow

Fluxo de autenticação JWT desde o login até a validação nas chamadas API.

```mermaid
sequenceDiagram
    autonumber
    actor User as Usuário (Rafael)
    participant SPA as Angular SPA
    participant GW as API Gateway
    participant Auth as AuthService
    participant DB as PostgreSQL
    participant CB as Core Banking
    participant AN as Analytics

    Note over User,AN: Authentication – JWT Token Flow

    rect rgb(248, 242, 240)
        Note left of Auth: Login Phase
    end
    
    User->>SPA: Acessa /login
    SPA->>SPA: Exibe formulário (email, password)
    
    User->>SPA: Submete credenciais
    SPA->>GW: POST /api/auth/login<br/>{email: "rafael@email.com", password: "••••••"}
    
    GW->>Auth: Forward credenciais (Secret/Shared Key)
    
    Auth->>DB: SELECT User WHERE email = 'rafael@email.com'
    DB-->>Auth: Return user + passwordHash (bcrypt)
    
    alt Credenciais válidas
        Auth->>Auth: Verify Hash (bcrypt.compare)
        Auth->>Auth: Generate JWT:
            - sub: userId
            - accountId: guid-123
            - role: "user"
            - exp: DateTime.UtcNow.AddHours(8)
            - iat: DateTime.UtcNow
        Auth->>GW: Return JWT (jwt.mini.token)
        GW-->>SPA: HTTP 200 + {accessToken: "jwt.xxx.yyy"}
        
        SPA->>SPA: Store token in localStorage/secure storage
        SPA->>SPA: Set HTTP Interceptor Authorization: Bearer {token}
        SPA->>User: Redireciona para Dashboard
    else Credenciais inválidas
        Auth->>GW: HTTP 401 Unauthorized
        GW-->>SPA: {error: "Credenciais inválidas"}
        SPA->>SPA: Show error toast
    end

    rect rgb(240, 248, 255)
        Note right of GW: API Calls with JWT
    end
    
    User->>SPA: Solicita transferência
    SPA->>GW: POST /api/transfers<br/>Authorization: Bearer {jwt}<br/>{toAccountToken, amount}
    
    GW->>GW: Validate JWT Signature (Microsoft.IdentityModel)
    
    alt JWT válido
        GW->>CB: Forward request + Claims Principal
        CB->>CB: Extract accountId from ClaimPrincipal
        CB->>CB: Use accountId as fromAccountId (trusted source)
    else JWT inválido/expirado
        GW-->>SPA: HTTP 401 Unauthorized
        SPA->>SPA: Redirect to login page
    end

    rect rgb(248, 242, 240)
        Note over AN: JWT Scope Validation
    end
    
    GW->>AN: GET /api/insights<br/>Authorization: Bearer {jwt}
    AN->>AN: Validate accountId in token matches requested resource
    
    alt Account mismatch
        AN-->>GW: HTTP 403 Forbidden
    else Valid
        AN->>AN: Process request
    end

    rect rgb(225, 245, 255)
        Note bottom of AN: Refresh Token Flow
    end
```

## JWT Configuration (.NET)

```csharp
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"])
            )
        };
    });

// Claims Added by AuthService
var claims = new[] {
    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
    new Claim("accountId", user.AccountId.ToString()),
    new Claim(ClaimTypes.Role, user.Role),
    new Claim("tenantId", user.AccountId.ToString()) // for multi-tenancy
};
```

## JWT Token Structure

### Header
```json
{ "alg": "HS256", "typ": "JWT" }
```

### Payload (Claims)
```json
{
  "sub": "uuid-user-id",
  "accountId": "uuid-account-id",
  "role": "user",
  "tenantId": "uuid-account-id",
  "iat": 1705312800,
  "exp": 1705356000,
  "nbf": 1705312800,
  "jti": "unique-jwt-id"
}
```

## Angular Interceptor

```typescript
@Injectable()
export class TokenInterceptor implements HttpInterceptor {
  constructor(private auth: AuthService, private router: Router) {}
  
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.auth.getToken();
    
    if (token) {
      const cloned = req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`)
      });
      return next.handle(cloned);
    }
    
    // Token expired?
    if (this.auth.isTokenExpired()) {
      this.router.navigate(['/login']);
      this.auth.logout();
    }
    
    return next.handle(req);
  }
}
```

## Auth Endpoints

| Endpoint | Método | Payload | Response |
| :--- | :--- | :--- | :--- |
| `/api/auth/login` | POST | `{email, password}` | `{accessToken, refreshToken, expiresIn}` |
| `/api/auth/refresh` | POST | `{refreshToken}` | `{accessToken, expiresIn}` |
| `/api/auth/logout` | POST | - | 204 No Content |

## Security Measures

| Camada | Medida | Descrição |
| :--- | :--- | :--- |
| **Password** | bcrypt | Hash com cost factor 12 |
| **JWT** | HS256/RS256 | Chave forte (256 bits) |
| **Token Expiry** | 8 hours | Renovação via refresh token |
| **HTTPS Only** | Obligatório | TLS 1.2+ em produção |
| **CORS** | Whitelist | Domínios permitidos |

## Token Validation Matrix

| Service | Validates | Uses |
| :--- | :--- | :--- |
| **Gateway** | JWT Signature + Expiry | Extract accountId for routing |
| **Core Banking** | `claim("accountId")` | fromAccountId para operação |
| **Analytics** | `claim("accountId")` == resource | Autorização de recursos |
| **Notifications** | `claim("accountId")` | Grupo SignalR: `account:{id}` |