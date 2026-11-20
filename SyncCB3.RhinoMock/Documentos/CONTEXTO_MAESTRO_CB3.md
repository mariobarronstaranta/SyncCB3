# Contexto maestro: integración SaeSync con Concrete Rhino (CB3)

**Revisado:** 2026-10-23  
**Propósito:** contexto de referencia para diseñar, implementar y mantener procesos de comunicación entre SaeSync y la API REST de Concrete Rhino.

## 1. Cómo usar este documento

Este archivo reúne el contexto común del contrato externo y del proyecto local. Antes de implementar un flujo, consultar también las fuentes detalladas:

- [Contrato de Concrete Rhino](CONTEXTO_API_CONCRETE_RHINO.md): rutas, autenticación, estructuras JSON, validaciones, errores y límites documentados por el proveedor.
- [Estado de implementación CB3](CONTEXTO_IMPLEMENTACION_CB3.md): arquitectura actual, código observado, dependencias, problemas y validación local.
- [Especificación original en inglés](Concrete_Rhino_REST_API_Specification.pdf) y [traducción al español](Especificación_de_la_API_REST_de_Concrete_Rhino.pdf).

Cuando las especificaciones difieran, el PDF original en inglés prevalece, según indica la traducción. No completar huecos del contrato con supuestos derivados de BCQCI, ULink, ejemplos del proveedor o comportamiento observado en una sola planta.

Este documento distingue:

- **Contrato:** comportamiento descrito en las especificaciones de la API.
- **Estado local:** comportamiento encontrado en el código y SQL de la solución.
- **Propuesta:** recomendación para una implementación futura; no significa que esté desarrollada ni aprobada por negocio.
- **Pendiente:** decisión que debe confirmarse antes de desplegar.

No contiene claves API, contraseñas ni datos reales de plantas.

## 2. Resumen ejecutivo

Concrete Rhino es un sistema de dosificación por planta con una API REST de versión `v1`. SaeSync debe tratar cada planta como un destino independiente: dirección, puerto, identidad, clave API, zona horaria y estado de software propios.

La comunicación no es un simple POST de JSON:

1. Cada solicitud necesita autenticación mediante API key.
2. La respuesta funcional se interpreta desde `request_status.status` en el JSON; el código HTTP por sí solo no basta.
3. Los cuerpos usan arreglos y envoltorios específicos por recurso.
4. PUT sirve para crear o actualizar cuando el recurso lo permite; POST es exclusivamente de alta.
5. Actualizar es parcial en general: los campos omitidos suelen conservarse. Algunas excepciones, como `plants` y `mixcomponents`, pueden borrar o reemplazar datos.
6. Los efectos operativos —en especial cambios de estado de pedidos y tickets— pueden cancelar, estacionar o modificar cargas e inventario.
7. El envío de escrituras puede quedar en estado incierto ante un timeout. No se debe reenviar ciegamente ni considerar cualquier conflicto como éxito.
8. La recepción de resultados requiere un cursor persistente por planta y una conciliación periódica por fecha.

La aplicación local `SaeSync.ClienteCB3` existe, pero sus flujos revisados no implementan todavía varios requisitos del contrato: API key, interpretación del estado interno, sincronización fiable, mapeo de datos real, tickets ni recolección de resultados.

## 3. Arquitectura local observada

El nombre real del proyecto es `SaeSync.ClienteCB3` (no `SaeSync.Clientes.CB3`). La aplicación es Windows Forms sobre .NET Framework 4.6.2.

```text
SaeSync.ClienteCB3 (Form1)
  ├─ SaeSync.Process (SyncCustomer)
  │    └─ SaeSync.DataAceess (DAOSYNC)
  │         └─ SQL Server / SyncCB3
  ├─ SaeSync.Entity (modelos compartidos)
  └─ HTTP ──> Concrete Rhino, instancia de una planta

SaeSync.DB ── objetos SQL / carga de Orders ──> SQL Server
```

Responsabilidades encontradas:

| Componente | Responsabilidad observada |
|---|---|
| `SaeSync.ClienteCB3` | UI manual, construcción de JSON, solicitudes HTTP y presentación |
| `SaeSync.Process` | Métodos de proceso que delegan lecturas a acceso a datos |
| `SaeSync.DataAceess` | Consultas SQL, materialización de entidades y bitácora de errores |
| `SaeSync.Entity` | Modelos compartidos; algunos restringen tipos que la API admite como texto/decimal |
| `SaeSync.DB` | DDL y procedimientos SQL; también contiene una rutina de carga a la base `SyncCB3` |

CB3 no referencia directamente el proyecto `SaeSync.DB`. La conexión con los objetos SQL es por la base configurada y procedimientos, no por una referencia de proyecto. Las bibliotecas compartidas tienen otros consumidores; antes de cambiar entidades o DAO, revisar el impacto en clientes, servicios y procesos existentes.

## 4. Estado funcional local comprobado

Se encontraron envíos manuales de clientes y pedidos, lecturas SQL y configuración de URLs. La compilación Debug del proyecto fue reportada satisfactoria en el análisis del 2026-10-23; eso no valida una planta real ni una sincronización completa.

| Capacidad | Estado observado |
|---|---|
| Lectura de clientes/pedidos desde SQL | Parcial, con dependencias externas y parámetros/valores fijos |
| Envío de clientes y pedidos | Manual; requiere adaptar al contrato |
| API key por solicitud | No observada en los flujos revisados |
| Evaluación de `request_status` | No observada |
| Envío de tickets y materiales/mezclas | No encontrado |
| Lectura de cargas/resultados | No encontrada en el cliente |
| Cursor incremental por planta | No encontrado |
| Persistencia de confirmaciones/reintentos | No encontrada en los flujos CB3 |
| Consulta de pedidos desde la UI | Controles presentes, evento de consulta no conectado |

Riesgos ya observados en los flujos locales:

- `ACCION_PEDIDOS` usa la URL del campo de clientes, aunque hay un campo de URL de pedidos.
- El cuerpo de pedidos escribe `delivey_addrs`; el nombre correcto del contrato es `delivery_addrs`. Campos desconocidos pueden ignorarse.
- Se observó `mix_code` fijo y valores sustituidos para cantidad/carga, en lugar de usar siempre los valores reales del pedido.
- Algunas lecturas usan la ciudad `MTY` fija y no pasan el filtro esperado.
- Se evalúa el éxito HTTP y se intenta leer una propiedad genérica `Message`, no `request_status`.
- El modelo local de código/número de cliente/pedido usa `int` en sitios donde Rhino permite identificadores alfanuméricos; cantidades enteras tampoco representan volúmenes fraccionarios.
- En la extracción, una excepción SQL puede convertirse en lista vacía o parcial, confundible con “sin datos”.

Estos hallazgos describen el estado examinado, no garantizan que no existan cambios posteriores.

## 5. Contrato de transporte y seguridad

Base documentada:

```text
http://<direccion-planta>:8080/rhino_rest_api/v1/json
```

El puerto depende de la instalación. JSON es el formato documentado. Autenticar cada solicitud con una API key en `Authorization` o `X-API-Key`; no ponerla en URL, logs, repositorio o mensajes al usuario. La especificación recomienda una clave por programa. Fuera de la red local se recomienda HTTPS o Tailscale.

Consideraciones:

- Una clave de lectura permite solo GET; se requiere permiso de lectura/escritura para mutaciones.
- Clave ausente, incorrecta o eliminada da error de autenticación. Diez fallos desde una dirección pueden bloquearla por un minuto; el proxy comparte ese bloqueo entre sus clientes.
- Una API key incorrecta no se resuelve reintentando rápidamente. Verificar configuración y detener el ciclo de escrituras.
- Consultar `status` al iniciar y validar que `plant_code`, versiones y destino correspondan a la planta configurada antes de mutar datos.
- Los códigos de planta no eligen el servidor: cada host es una instancia separada.
- Usar rutas sin barra final por compatibilidad con plantas antiguas.
- Codificar segmentos URL con cuidado: `+` debe codificarse como `%2B`; los códigos con `/` no se pueden usar como segmentos, ni codificándolos como `%2F`.
- No inventar filtros de consulta o paginación mediante parámetros `?`; los endpoints documentados no los usan.
- Fechas en rutas como `YYYY-MM-DD`; rangos inclusivos, máximo 93 días para pedidos.
- Los nombres de propiedades son sensibles a mayúsculas/minúsculas. Errores de campo pueden ignorarse en lugar de fallar.

## 6. Interpretación de respuestas y reintentos

El cuerpo puede llegar con HTTP 200 aunque la operación haya fallado. La autoridad funcional es `request_status.status`, acompañado de `msg`, `detail` y, a veces, `error_detail`.

| Estado API | Interpretación general |
|---|---|
| 200 | Lectura con datos o DELETE exitoso |
| 201 | Alta o actualización aceptada |
| 204 | Consulta sin resultados; el código está dentro del JSON |
| 400 | Datos, JSON, ruta o validación incorrectos |
| 401 | Autenticación fallida |
| 403 | Clave de solo lectura o planta/destino incorrecto |
| 404 | Recurso o registro no encontrado; depende de la ruta |
| 405 | Método no permitido, por ejemplo DELETE no soportado en ese recurso |
| 409 | Conflicto: duplicado o incompatibilidad operativa/de datos |
| 501 | Método no implementado |
| 503 | Base ocupada; puede reintentarse con espera limitada |

El cliente debe distinguir errores de transporte, HTTP no exitoso, JSON inválido y respuesta sin `request_status`. Conservar `error_detail` para diagnóstico, pero no depender de comparar literalmente mensajes en inglés.

**Propuesta:** resultado tipado con HTTP status, estado API, `msg`, `detail`, `error_detail` y payload. Un timeout durante PUT/POST significa “resultado remoto desconocido”: consultar el recurso/estado antes de reenviar. Reintentar 503 con backoff y límite; no reintentar automáticamente 401/403 ni convertir cualquier 409 en éxito.

## 7. Recursos principales y reglas que afectan el diseño

Rutas relativas a la base, según el inventario del proveedor:

| Recurso | Rutas comunes | Uso |
|---|---|---|
| Estado | `status`, `status/<result_id>` | Identidad, versión, reloj y progreso de resultados |
| Catálogos | `plants`, `customers`, `materials`, `mixdesigns` | Consultar y, según recurso, crear/actualizar |
| Pedidos | `orders`, `orders/<order_num>`, `orderslist/...` | Consultar, crear/actualizar |
| Estado/cargas de pedido | `orderstatus/<order_num>`, `orderloads/<order_num>` | Consultar y cambiar estado; ver cargas asociadas |
| Tickets | `tickets`, `tickets/<ticket_num>` | Encolar/actualizar una carga lista para dosificar |
| Cargas | `loads`, `loads/<date>`, `loads/<ticket>` | Consultar operación y estado de cargas |
| Resultados | `batchresults`, `batchresults/<date>`, `batchresults/since/<id>` | Leer valores de dosificación |

Detalles relevantes:

- POST es de alta y un duplicado normalmente responde conflicto; PUT es la modalidad de alta/actualización cuando el endpoint la admite.
- Las escrituras suelen validar el lote completo antes de aplicarlo, pero `plants` es excepción y puede dejar cambios parciales.
- Los campos omitidos generalmente conservan el valor existente; no serializar todos los defaults indiscriminadamente.
- `plants` tiene comportamiento destructivo para campos omitidos.
- `mixcomponents` enviado reemplaza el diseño completo; omitirlo permite modificar solo `mixinfo`.
- `cust_udf2` tiene semántica incompatible entre lectura y escritura; no reenviarlo desde un GET hasta aclararlo con el proveedor.
- Lecturas y escrituras no tienen siempre la misma forma/campos. Ejemplo: en pedidos, `quantity` de escritura se refleja como `qty_required` y `mix_price` puede aparecer como `price`.
- Estados de pedidos y cargas son dominios separados. Completar/cancelar pedido puede afectar cargas e inventario.
- Los tickets pueden crear automáticamente camión u operador bajo ciertas reglas; conviene validar/conciliar esas altas.
- Enviar un `ticket_num` estable si existe. Tickets sin número no pueden actualizarse/eliminarse por la API.
- No enviar dos tickets del mismo pedido en una sola solicitud; deben ir separados. Los extras enviados reemplazan los del ticket, no se suman.
- Un ticket pone la carga en estado Ready; la dosificación la inicia la operación de planta.
- Ver el documento del contrato para estructuras exactas, campos, límites, defaults, estados y efectos por endpoint.

## 8. Fechas, números, textos y unidades

- Enviar fechas ISO `yyyy-MM-dd`, horas de 24 horas y números con formato independiente de la cultura regional.
- La API admite que algunos números se representen como texto al leer; los DTO no deben depender de una sola representación sin pruebas.
- Validar longitudes y reglas del contrato antes de enviar. La documentación difiere sobre si los límites de texto son bytes o caracteres (especialmente con acentos/ñ); confirmar con el proveedor y no truncar UTF-8 de forma insegura.
- No asumir que una planta en México usa unidades métricas. `mix_unittype` controla US/yd³ frente a métrico/m³ y las unidades de componentes dependen del tipo de material.
- Mantener la distinción de cantidades de diseño, objetivo y real (`bc_design`, `bc_target`, `bc_actual`), así como sus unidades.
- `mix_mixtime` se expresa en minutos mientras que la lectura de pedido `mix_time` aparece en segundos.
- Confirmar decimales, redondeos, moneda, slump y magnitudes permitidas con negocio/planta.

## 9. Pedidos y mapeo desde SaeSync

`order_num` en Rhino es texto de hasta 31 caracteres y no debe equipararse automáticamente con `IDPedido`. El contrato exige cliente, mezcla y cantidad positiva; el `mix_code` debe existir en la planta.

Mapeos locales candidatos, todavía no aprobados:

| Campo Rhino | Fuente SaeSync candidata | Validación pendiente |
|---|---|---|
| `plant_code` / host / clave | `Plantas`, configuración de destino | Relación exacta entre código, host, clave y zona horaria |
| `cust_code` | `Clientes.ClaveCliente` + ciudad | Evitar confundir con ID numérico y preservar ceros iniciales |
| `order_num` | `Pedidos.IDPedido` / pedido padre-hijo | Definir grano y unicidad por planta |
| `mix_code` | Producto/mezcla del pedido | Correspondencia con diseño de mezcla existente en Rhino |
| `quantity` | `PedidoProducto.Cantidad` u origen acordado | Unidad, precisión, cantidad total frente a carga |
| `load_size`, `num_loads` | Viajes/programación | Tamaño de carga y reglas de redondeo |
| `delivery_addrs` | `Obra` | Dirección de entrega correcta y máximo de texto |
| `ticket_num` | Remisión/viaje/cierre | Rhino exige entero positivo; remisiones locales pueden ser alfanuméricas |
| `truck_num`, `driver_name` | `Unidad`, `Personal` | Códigos de planta, longitudes y alta automática |

El procedimiento `CARGA_ORDENES_CB3` repuebla `SyncCB3.dbo.Orders` con filtros/valores fijos observados, hace DELETE global de esa tabla y no se encontró conectado al botón de envío CB3. `sp_Customers_GetAll` y `Sp_Orders_GetToday` no aparecieron en los scripts revisados; documentar su esquema/origen antes de depender de ellos.

### Advertencia sobre identificadores de viaje

En el DDL, `Viaje.IDViaje` y `Viaje.Id_Viaje` son columnas distintas; `Id_Viaje` es IDENTITY. `InsertaViajes_SEQ` asigna el valor de `SEQ_VIAJE` a `IDViaje` y lo usa en `PedidoViaje`/`PedidoViajeCierre`. La consulta `sp_SincronizarViajesCierres` une estas relaciones con `Viaje.Id_Viaje`. Verificar esta discrepancia antes de usar ese procedimiento como fuente de sincronización: los registros creados por la ruta con secuencia podrían no coincidir si los dos IDs no son iguales.

## 10. Lectura incremental de resultados

Los resultados terminados incluyen `bt_resultid`, un identificador incremental por planta; `0` indica que aún no terminó y no sirve como identidad única para pendientes.

Flujo de contrato:

1. Consultar `status` y persistir el último `bt_resultid` procesado por planta.
2. Consultar `status/<cursor>` para conocer pendientes.
3. Obtener `batchresults/since/<cursor>`; vienen resultados terminados posteriores al cursor, en orden ascendente, hasta 100 por respuesta.
4. Guardar resultados y cursor juntos en una transacción local; repetir mientras `more=1`.
5. Tratar 204 como respuesta funcional “sin resultados”, no como cuerpo HTTP vacío.
6. Hacer conciliación diaria por fecha local de planta: una carga completada manualmente tras interrupción puede no aparecer como nueva después del cursor actual.

**Propuesta:** clave de deduplicación de resultados terminados basada en identidad de planta + `bt_resultid`, con actualización en conciliación diaria. Definir separadamente cómo identificar cargas con `bt_resultid=0`, y cómo actuar ante cambio de equipo/restauración o retroceso del cursor; el proveedor no especifica completamente esos casos.

## 11. Persistencia y operación recomendadas

No existe en los flujos CB3 revisados una cola durable con confirmaciones y reintentos. Las tablas `Sync_Ejecucion` y `Sync_Detalle` de `SaeSync.DB` dan una bitácora básica, pero no modelan por sí solas un cursor por planta ni los componentes dosificados.

Antes de automatizar, diseñar persistencia para:

- destino/planta y recurso;
- clave de negocio y correlación con SaeSync;
- petición lógica sin secretos;
- número de intento, timestamps, HTTP status y estado API por separado;
- resultado/error del proveedor;
- estado de procesamiento e idempotencia;
- cursor de resultados por planta.

No guardar la API key ni loguear datos personales innecesarios. Evitar que ejecuciones concurrentes de la misma planta/pedido alteren orden o reenvíen datos. Diseñar retención y recuperación operacional.

## 12. Siguiente secuencia de trabajo sugerida

Esta secuencia orienta el desarrollo; no es una decisión funcional ni una implementación ya realizada.

1. Confirmar plantas, versiones, base URLs, credenciales protegidas, zonas horarias y permisos. Verificar identidad con `status`.
2. Acordar el alcance: clientes, pedidos, tickets, materiales/mezclas y/o recepción de resultados; definir sistema propietario de cada dato.
3. Acordar mapeos y unidades con negocio, planta y proveedor; resolver remisiones alfanuméricas, IDs, direcciones, precios y efectos fiscales.
4. Crear cliente HTTP separado del formulario, DTO específicos por endpoint, autenticación y resultado tipado.
5. Corregir extracción SQL y mapeos; separar “sin datos” de fallos SQL y retirar valores de prueba.
6. Implementar primero lecturas/validaciones de destino y catálogos; después escrituras con PUT/POST elegidos por semántica.
7. Incorporar registro durable de confirmaciones y manejo de escrituras inciertas antes de habilitar reintentos automáticos.
8. Implementar tickets con correlación estable y serialización por pedido.
9. Implementar recolección de resultados con persistencia transaccional del payload y cursor, más conciliación por fecha.
10. Añadir pruebas de contrato/mapeo y, finalmente, pruebas controladas contra una planta autorizada.

## 13. Criterios mínimos de verificación

- Un HTTP 200 con estado API 400/401/403/409/503 se identifica como fallo; 201 de actualización como éxito; 204 como sin resultados.
- Respuestas HTML, vacías, JSON inválido o sin `request_status` no se convierten en éxito.
- Se prueban las estructuras JSON exactas, incluyendo singular/lista, pedidos, estados, `loads` con envoltorio doble y resultados singulares por ticket.
- Se verifican omisión vs `null`/cadena vacía, límites, mayúsculas, acentos, códigos y unidades.
- Se prueba lote inválido y se verifica atomicidad, excepto el endpoint especial de plantas.
- Timeout de escritura no genera reenvío ciego; 503 respeta backoff/límite.
- Se prueban tickets duplicados, iniciados, cancelados, sin número, reutilizados y varios tickets del mismo pedido.
- Reiniciar el consumidor entre persistir resultado y cursor no pierde ni duplica procesamiento.
- Una página de 100 resultados con `more=1` seguida de otra consulta/204 se procesa correctamente.
- Las pruebas de sincronización verifican la correspondencia entre `Viaje.IDViaje` y `Viaje.Id_Viaje`.

## 14. Decisiones pendientes antes de producción

- Plantas/destinos iniciales, versión mínima instalada, URLs/puertos, credenciales y gestión/rotación de claves.
- Alcance funcional y sistema maestro para clientes, pedidos, mezclas, tickets y resultados.
- Mapeo de ciudad/planta, pedido padre/hijo, cliente, mezcla, producto y dirección de entrega.
- Correlación de remisiones alfanuméricas con tickets numéricos, y política de ticket reutilizado.
- Unidad/precisión de volumen, precio, moneda, slump, agua y campos fiscales.
- Cómo tratar cambios hechos directamente en Rhino y conflictos entre sistemas.
- Cursores iniciales, retención histórica, conciliación y recuperación después de una restauración.
- Frecuencias, concurrencia y timeouts: no inventar límites generales no especificados por la API.
- Confirmación de la discrepancia de IDs del procedimiento SQL de sincronización.

## 15. Límites y mantenimiento

La revisión es estática y documental. No se consultó una instancia de Concrete Rhino ni se probaron credenciales, datos, endpoints o resultados. Una compilación exitosa solo confirma compilación.

Al cerrar cada cambio funcional:

1. Actualizar este resumen y el [contexto de implementación](CONTEXTO_IMPLEMENTACION_CB3.md) con lo que realmente quedó implementado.
2. Actualizar el [contrato API](CONTEXTO_API_CONCRETE_RHINO.md) solo ante una nueva especificación o aclaración verificable del proveedor.
3. Separar explícitamente propuesta, decisión aprobada y comportamiento probado.
4. Registrar pruebas y límites de validación; nunca incluir secretos.
5. Enlazar los archivos SQL, código, configuración y pruebas que sean fuente de verdad.

### Historial

| Fecha | Cambio |
|---|---|
| 2026-10-23 | Contexto maestro inicial a partir de las especificaciones PDF y los análisis de contrato e implementación CB3 |
