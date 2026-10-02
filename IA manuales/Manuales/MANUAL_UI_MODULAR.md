# MANUAL — CONSTRUCCIÓN MODULAR DE UI Y VISTAS PARA AGENTES IA

FORMATO: 1.0

ALCANCE: Genérico. Aplicable a cualquier sistema web MVC (la implementación de
referencia usa Razor/ASP.NET; los conceptos se adaptan a cualquier motor de
vistas: Blade, ERB, Thymeleaf, Jinja, etc.).

---

## 1. Cómo usar este manual

Este documento es un prompt autocontenido para agentes IA que construyan,
modifiquen o revisen interfaces. Define UNA forma de construir vistas:

> Las pantallas se COMBINAN a partir de piezas existentes.
> Nunca se escriben de cero.

Al iniciar una tarea de UI, el agente debe:

1. Leer este manual completo (una vez).
2. Verificar qué sistema visual existe en el proyecto (sección 4).
3. Aplicar el workflow de la sección 9 en cada tarea.

Si el proyecto tiene su propio Design System documentado, gana ese documento;
este manual define el MÉTODO, no la identidad visual específica.

---

## 2. Filosofía

Un sistema no es una colección de pantallas: es UNA interfaz con muchas
combinaciones.

Cada pantalla debe ayudar al usuario a trabajar mejor, pensar menos y terminar
más rápido. La UI transmite: claridad, orden, calma, velocidad, confianza.

Para lograrlo a escala, la construcción es MODULAR:

- **Tokens**: el ADN visual (colores, tipografía, espaciado).
- **Componentes**: órganos reutilizables que resuelven un problema.
- **Vistas**: ensamblajes delgados que solo deciden QUÉ contenido va dónde.
- **Páginas**: hojas de estilo excepcionales, solo cuando un módulo lo exige.

La regla madre:

> La vista decide el CONTENIDO. El componente decide la ESTRUCTURA.
> El token decide el VALOR VISUAL.

---

## 3. Principios fundamentales

1. **DRY absoluto**: si un patrón HTML aparece en dos vistas, se extrae a un
   componente. Si un estilo aparece en dos páginas, sube al Design System.
2. **Thin Views**: las vistas contienen la mínima lógica posible. No llaman
   servicios ni repositorios. Solo muestran y ensamblan.
3. **Responsabilidad única**: cada archivo hace UNA cosa (controller coordina,
   service aplica reglas, vista muestra, parcial es componente).
4. **Componentes bajo demanda**: NO se crean componentes por adelantado. Se
   crean cuando existe necesidad REAL detectada por repetición.
5. **Los componentes crecen, las pantallas no**: ante un nuevo requisito, se
   extiende el componente (nuevo slot/opción), no se copia y pega la pantalla.
6. **Cero valores mágicos**: ningún color, medida o fuente escrita a mano.
   Todo referencia a una variable del sistema de tokens.
7. **Una sola acción primaria**: nunca dos botones primarios compitiendo.
8. **El responsive pertenece al Layout**: los módulos no deciden cómo
   comportarse en mobile; el shell lo hace por todos.

---

## 4. Arquitectura de capas de UI

Cuatro capas, de abajo hacia arriba. Cada capa SOLO conoce a la inferior:

```
CAPA 4 - VISTAS (views/{modulo}/pagina.ext)
         Ensamblan componentes + datos. Delgadas.

CAPA 3 - PÁGINAS (pages/{modulo}.css)      [excepcional]
         Estilos SOLO de un módulo, cuando lo justifica.

CAPA 2 - COMPONENTES (shared/components/_X.ext + components.css)
         Piezas reutilizables con clases del DS.

CAPA 1 - DESIGN SYSTEM (tokens.css + components.css + layout.css)
         Tokens = fuente única de verdad visual. AppShell = estructura global.
```

### Implementación de referencia (Razor / ASP.NET)

```
wwwroot/
├── css/
│   ├── tokens.css            <- CAPA 1: variables :root (--app-accent, --app-bg...)
│   ├── app-components.css    <- CAPA 1+2: clases .app-btn, .app-card, .app-input...
│   ├── app-layout.css        <- CAPA 1: AppShell (sidebar/topbar/content/drawers)
│   └── pages/
│       └── app-{modulo}.css  <- CAPA 3: único CSS permitido por módulo
Views/
├── Shared/
│   ├── _Layout.cshtml        <- AppShell: renderiza shell + capas globales
│   └── Components/
│       ├── _PageHeader.cshtml    <- CAPA 2: componentes reutilizables
│       ├── _KpiCard.cshtml
│       ├── _EmptyState.cshtml
│       ├── _FormLayout.cshtml
│       └── _TableActions.cshtml
└── {Modulo}/
    └── Index.cshtml          <- CAPA 4: vista delgada que compone
```

Prefijo de clases: el proyecto elige uno (`jk-`, `app-`, `ls-`...). Todas las
clases del sistema llevan el prefijo; nada suelto sin namespace.

---

## 5. El Design System (Capa 1)

### tokens.css — fuente única de verdad

Define como variables CSS, con comentarios de intención:

```css
/* Do NOT hardcode values. Use these variables everywhere. */
:root {
  /* Color */
  --app-bg: #f5f5f7;            /* fondo general */
  --app-surface: #ffffff;       /* tarjetas, paneles */
  --app-border: #e8e8ed;
  --app-text-primary: #1d1d1f;
  --app-text-secondary: #86868b;
  --app-accent: #2563eb;        /* acción primaria */
  --app-success: #34c759;
  --app-warning: #ff9500;
  --app-danger: #ff3b30;

  /* Tipografía (escala fija, nunca tamaños arbitrarios) */
  --app-font-sans: 'Inter', system-ui, sans-serif;
  --app-text-sm: 0.8125rem;
  --app-text-base: 0.875rem;
  --app-text-lg: 1.125rem;
  --app-text-xl: 1.25rem;

  /* Espaciado (escala base 8px; nada aleatorio) */
  --app-space-2: 8px;
  --app-space-3: 16px;
  --app-space-4: 24px;
  --app-space-5: 32px;

  /* Forma y profundidad */
  --app-radius: 10px;
  --app-shadow: 0 1px 3px rgba(0,0,0,.06);
}
```

Identidad visual recomendada: claridad > decoración. Sin gradientes fuertes,
sin sombras agresivas, todo limpio (inspiración: Apple/Notion/Linear/Vercel).

### app-components.css — vocabulario de clases

Clases para primitivas: `.app-btn` (+ variantes primary/ghost/danger),
`.app-card`, `.app-input`, `.app-table`, `.app-badge`, `.app-empty`,
utilidades de espaciado (`.app-mt-24`) y visibilidad responsive
(`.app-hide-mobile`). Si una clase nueva se necesita en 2 módulos → nace aquí,
no en la página.

### AppShell — estructura global

Toda pantalla vive dentro del mismo shell:

```
Topbar
Sidebar          <- representa el NEGOCIO (lenguaje del usuario),
Content Area        nunca tablas de BD ni palabras técnicas ("CRUD", "Tabla")
Drawer Layer
Toast Layer
Modal Layer
```

Los módulos se renderizan ÚNICAMENTE dentro del Content Area.
Zonas fijas: nunca se modifican desde un módulo.

---

## 6. Componentes (Capa 2)

### Anatomía obligatoria de un componente

```razor
@*
    {Proyecto} - {Nombre} Component
    Resuelve: {problema concreto que resuelve}
    Usage:
    @Html.Partial("~/Views/Shared/Components/_PageHeader.cshtml",
        new ViewDataDictionary {
            { "Title", "Clientes" },
            { "Subtitle", "Administra tus clientes" },
            { "PrimaryAction", Url.Action("Create") },
            { "PrimaryActionLabel", "Nuevo Cliente" }
        })
*@

@{
    string title = ViewBag.Title ?? "";
    string subtitle = ViewBag.Subtitle ?? "";
    string primaryAction = ViewBag.PrimaryAction ?? "";
}

<div class="app-page-header">
  ...estructura con clases del DS...
  @if (!string.IsNullOrEmpty(primaryAction)) { ...render condicional... }
</div>
```

Reglas del contrato:

1. **Header de documentación**: todo componente empieza comentado con nombre,
   problema que resuelve y ejemplo de uso. Un agente posterior debe poder
   usarlo sin leer su interior.
2. **Slots con fallback**: cada slot lee de ViewBag/ViewData con valor por
   defecto (`?? ""`). Slot vacío = sección no se renderiza.
3. **Render condicional**: las partes opcionales se ocultan solas; el caller
   nunca envía HTML vacío.
4. **Solo clases del DS**: cero estilos inline, cero colores literales.
5. **Componente inteligente**: resuelve un problema, no es HTML suelto
   reutilizado. Acepta título/desc/ícono/color/url/estado/CTA según corresponda.
6. **Catálogo vivo**: cada componente nuevo se registra en el diccionario/catálogo
   del proyecto (ver sección 10).

### Catálogo mínimo recomendado

PageHeader · SectionHeader · KpiCard · ActionCard · Card · Table (+TableActions)
· SearchBar/Filters · FormLayout (+FormActions) · DeleteLayout · EmptyState ·
Drawer · Toast · Modal · Timeline · Avatar · Badge · FloatingButton ·
QuickCreateDrawer.

Estos cubren ~90% de las pantallas CRUD de cualquier sistema administrativo.

---

## 7. El patrón de composición (Capa 4)

Una vista NO es un HTML monolítico. Se compone así:

```razor
@model List<Cliente>

@* 1. Helpers: bloques de CONTENIDO de esta vista *@
@helper ListContent() {
    <div class="app-table-wrap">
        <table class="app-table">
            @foreach (var item in Model) {
                <tr>
                    <td>@item.Nombre</td>
                    <td>@item.Telefono</td>
                    <td>
                        @Html.Partial("~/Views/Shared/Components/_TableActions.cshtml",
                            new ViewDataDictionary {
                                { "EditUrl", Url.Action("Edit", new { id = item.Id }) },
                                { "DeleteUrl", Url.Action("Delete", new { id = item.Id }) },
                                { "ItemName", item.Nombre }
                            })
                    </td>
                </tr>
            }
        </table>
    </div>
}

@* 2. Slots: asignar contenido al componente contenedor *@
@{
    ViewBag.Title = "Clientes";
    ViewBag.Subtitle = "Administra tus clientes";
    ViewBag.PrimaryAction = Url.Action("Create");
    ViewBag.PrimaryActionLabel = "Nuevo Cliente";
    ViewBag.PageContent = ListContent();
}

@* 3. Render final: el componente decide la estructura *@
@Html.Partial("~/Views/Shared/Components/_CrudPage.cshtml")
```

Flujo mental para el agente:

1. ¿Qué DATOS muestra? → `@model`
2. ¿Qué COMPONENTES existen que ya resuelven esto? → buscarlos primero
3. ¿Qué contenido propio falta? → helper(s)
4. ¿Dónde va cada cosa? → slots del componente contenedor
5. Renderizar UN componente raíz (CrudPage/FormLayout/PageHeader...)

---

## 8. Reglas UX transversales

Aplican a toda pantalla, independiente del stack:

1. **PageHeader siempre**: título + subtítulo + acción principal. Nunca se omite.
2. **Una sola acción primaria** por pantalla; secundarias en estilo ghost.
3. **Nunca pantalla vacía**: todo empty state explica qué pasa, por qué, y
   ofrece LA acción ("No tienes clientes." → [Crear Cliente]).
4. **Drawers > páginas nuevas** para edición/inspección contextual: el usuario
   no pierde el contexto. Modales SOLO para confirmaciones y acciones críticas,
   jamás formularios largos.
5. **Tablas son secundarias**: priorizar cards/timelines/widgets; tabla solo
   cuando la densidad lo justifica. Toda lista tiene variante mobile.
6. **Feedback siempre**: toast tras toda acción; estado de carga visible.
7. **Lenguaje del usuario** en labels/navegación; jamás jerga técnica.
8. **Responsive por capa**: desktop/tablet/mobile lo resuelve el shell y los
   componentes; la vista no escribe media queries propias.

---

## 9. Workflow del agente en cada tarea de UI

ANTES de escribir código:

1. **Inventario**: listar componentes existentes y clases del DS aplicables al
   requerimiento. Buscar patrones similares ya resueltos en otras vistas.
2. **Decidir estrategia**:
   - Existe componente que resuelve → USARLO.
   - Existe casi → EXTENDERLO con slot/variante (sin romper usos previos).
   - Patrón nuevo Y repetido en ≥2 lugares → CREAR componente.
   - Estilo exclusivo de un módulo → CAPA 3 (`pages/{modulo}.css`), con tokens.
   - Valor visual nuevo → TOKEN nuevo en tokens.css, con nombre semántico.
3. Si dudás entre crear componente o duplicar: duplicar está PROHIBIDO; si el
   patrón aún aparece una sola vez, mantenelo inline EN LA VISTA y extraélo
   apenas aparezca la segunda vez (no antes: no hay componentes especulativos).

DURANTE:

4. Vista delgada: model + helpers + slots + un render final. Sin lógica de negocio.
5. Todo valor visual vía token. Toda pieza repetida vía componente.
6. Accesibilidad básica: jerarquía h1→h2, labels en inputs, foco visible,
   contraste suficiente.

DESPUÉS:

7. Verificar contra el checklist (sección 12).
8. Registrar: componente nuevo → catálogo; token nuevo → documentación del DS;
   decisión estructural → memoria del proyecto (si existe, AI/02_DECISIONES.md).
9. Compilar/build y verificar en los 3 breakpoints (desktop/tablet/mobile).

---

## 10. Catálogo de componentes (documento vivo)

Mantener un archivo `{PROYECTO}-UI-Catalog.md` (o sección en Docs/) que liste
cada componente: nombre, problema que resuelve, slots aceptados, ejemplo de uso
y dónde se usa hoy. Es el índice que evita duplicados: ANTES de crear un
componente, el catálogo ES la búsqueda.

Formato por entrada:

```
### _NombreComponente
Resuelve: ...
Slots: Slot1 (default) | Slot2 | ...
Uso: snippet
Usado en: Modulo/Accion, Modulo/Accion
```

---

## 11. Beneficios de este enfoque (por qué existe)

1. **Consistencia total**: todas las pantallas se ven y se sienten iguales;
   el usuario aprende una vez.
2. **Velocidad de desarrollo**: una pantalla nueva es un ensamblaje de minutos,
   no una construcción desde cero.
3. **Mantenimiento barato**: cambiar el color de marca o un radio = editar UN
   variable en tokens.css; corregir un bug de botones = tocar UN componente.
4. **Escalabilidad sin caos**: agregar módulo N° 30 cuesta igual que el N° 3.
5. **Reducción de errores**: menos HTML duplicado = menos lugares donde fallar.
6. **Vistas auditables**: al ser delgadas, revisar una vista toma segundos.
7. **IA-friendly**: el agente trabaja con un VOCABULARIO ACOTADO (catálogo +
   tokens). Menos libertad = menos alucinaciones visuales y salidas predecibles.
   Este es el beneficio clave para agentes: el DS convierte la generación de UI
   abierta en una tarea de selección y composición.
8. **Onboarding instantáneo**: humano o modelo nuevo entiende el sistema leyendo
   el catálogo, no el código completo.

---

## 12. Checklist de validación de UI

Antes de dar por terminada una pantalla:

- [ ] Usa el AppShell; nada fuera del Content Area.
- [ ] Empieza con PageHeader (título + subtítulo + acción primaria).
- [ ] Una sola acción primaria; secundarias ghost.
- [ ] Cero colores/medidas hardcodeados: todo vía tokens.
- [ ] Cero HTML duplicado: todo patrón repetido usa componente existente.
- [ ] Empty states con explicación + acción.
- [ ] Toast/feedback tras acciones; estados de carga.
- [ ] Variante mobile resuelta por clases del sistema, no media queries locales.
- [ ] Lenguaje de usuario (sin "CRUD", "tabla", IDs técnicos en UI).
- [ ] Componente/token nuevos registrados en catálogo/documentación.
- [ ] Build pasa y se verificó en desktop/tablet/mobile.

---

## 13. Antipatrones (qué NUNCA hacer)

1. **HTML monolítico**: una vista de 300 líneas con estructura propia.
2. **Colores literales** (`style="color:#dc3545"`) o estilos inline.
3. **CSS por página suelto** fuera de la capa Pages (o peor: `<style>` en la vista).
4. **Componentes especulativos**: crear "_CardGenericaV2" por si acaso.
5. **Copiar-pegar y ajustar**: clonar un componente en vez de extenderlo con slot.
6. **Dos primarios**: dos botones destacados compitiendo en una pantalla.
7. **Formularios en modales** largos; modales para editar entidades completas.
8. **Media queries en la vista/módulo**: el responsive es del layout/componente.
9. **Romper el shell**: un módulo que dibuja su propia sidebar/topbar/toast.
10. **Silencio**: acciones sin feedback o pantallas vacías sin guía.

---
---

# PLANTILLAS DE ARRANQUE

Para proyectos SIN Design System aún. Copiar y renombrar prefijo.

## PLANTILLA A — tokens.css mínimo viable

```css
/*
   {Proyecto} - Design Tokens
   Single source of truth for all visual variables.
   Do NOT hardcode values. Use these variables everywhere.
*/
:root {
  --{p}-bg: #f5f5f7;
  --{p}-surface: #ffffff;
  --{p}-border: #e8e8ed;
  --{p}-text-primary: #1d1d1f;
  --{p}-text-secondary: #86868b;
  --{p}-accent: #2563eb;
  --{p}-accent-hover: #1d4ed8;
  --{p}-success: #34c759;
  --{p}-warning: #ff9500;
  --{p}-danger: #ff3b30;
  --{p}-font-sans: 'Inter', system-ui, sans-serif;
  --{p}-text-sm: 0.8125rem;
  --{p}-text-base: 0.875rem;
  --{p}-text-lg: 1.125rem;
  --{p}-text-xl: 1.25rem;
  --{p}-space-2: 8px;
  --{p}-space-3: 16px;
  --{p}-space-4: 24px;
  --{p}-radius: 10px;
  --{p}-shadow: 0 1px 3px rgba(0,0,0,.06);
}
```

(`{p}` = prefijo del proyecto: app, ls, jk...)

## PLANTILLA B — componente genérico (Razor)

```razor
@*
    {Proyecto} - {Nombre} Component
    Resuelve: {problema concreto}.
    Usage:
    @Html.Partial("~/Views/Shared/Components/_{Nombre}.cshtml",
        new ViewDataDictionary {
            { "Slot1", valor },
            { "Slot2", valorOpcional }   // opcional: no renderiza si falta
        })
*@
@{
    string slot1 = ViewBag.Slot1 ?? "";
    string slot2 = ViewBag.Slot2 ?? "";
}

<div class="{p}-{nombre}">
    <div class="{p}-{nombre}__slot1">@slot1</div>
    @if (!string.IsNullOrEmpty(slot2))
    {
        <div class="{p}-{nombre}__slot2">@slot2</div>
    }
</div>
```

## PLANTILLA C — vista compuesta (Razor)

```razor
@model {TipoDelModelo}

@helper PageContent() {
    @* CONTENIDO de esta pantalla, usando componentes existentes *@
}

@{
    ViewBag.Title = "{Título}";
    ViewBag.Subtitle = "{Subtítulo}";
    ViewBag.PrimaryAction = Url.Action("{Accion}");
    ViewBag.PrimaryActionLabel = "{Etiqueta}";
    ViewBag.PageContent = PageContent();
}

@Html.Partial("~/Views/Shared/Components/_{Contenedor}.cshtml")
```

---

*Fin del manual. Copiar a la raíz de cualquier proyecto junto con
MANUAL_MEMORIA_AGENTES.md; referenciarlo en las rutas de lectura de tareas
de UI del PROMPT_INICIO.*
