# Contexto de integración: Concrete Rhino REST API

Fecha de análisis: 2026-10-23. API: `v1`. Revisión documental: `2026-10-20`.

## 1. Propósito y fuentes

Este archivo conserva el conocimiento necesario para desarrollar los procesos de comunicación de SaeSync con Concrete Rhino Automated Batching System, de Automation Logic, LLC. Leerlo antes de implementar clientes HTTP, DTO, validaciones, sincronización o persistencia relacionada con CB3.

Fuentes locales revisadas:

- [EN: Concrete Rhino REST API Specification](Concrete_Rhino_REST_API_Specification.pdf), 35 páginas, documento original.
- [ES: Especificación de la API REST de Concrete Rhino](Especificación_de_la_API_REST_de_Concrete_Rhino.pdf), 40 páginas, traducción.

**Prevalece EN si las versiones difieren**, como establece ES p. 1. Las referencias de este archivo usan las secciones originales, comunes a ambos PDF, y páginas EN salvo indicación contraria. Los nombres de campos, rutas y mensajes se conservan en inglés.

La API deriva de BCQCI 3.4 (noviembre de 2017), pero no implementa toda esa especificación. No inferir funciones de BCQCI o de ULink que no estén descritas aquí. Los ejemplos del proveedor no son configuración real de nuestras plantas.

Alcance de esta revisión: documentos completos y contraste puntual con `Form1.cs`, `ConfigReader.cs`, el proyecto CB3 y el DDL de SaeSync.DB. No se probaron servidores, credenciales, versiones instaladas ni datos reales. Las secciones de propuestas y mapeos pendientes no constituyen decisiones aprobadas de negocio.

## 2. Reglas esenciales para futuras implementaciones

1. Cada computadora de planta tiene su propia API, dirección, clave e identidad. No existe un código de planta en la ruta que permita cambiar de destino.
2. El resultado de aplicación se lee en **`request_status.status`**. Un HTTP 200 no significa éxito de la operación.
3. Toda solicitud lleva una API key. No hay un flujo de login/token temporal documentado.
4. `PUT` crea o actualiza donde el recurso lo permite; `POST` solo crea. Los duplicados por POST son conflictos.
5. Los cuerpos son objetos JSON con arreglos y envoltorios específicos del recurso; no enviar arreglos desnudos.
6. Omitir un campo normalmente conserva su valor; no serializar indiscriminadamente valores por defecto. `plants` es una excepción destructiva respecto de campos omitidos.
7. Una escritura multirregistro es atómica, excepto `plants`. Esto no equivale a una transacción entre distintas llamadas ni entre Rhino y nuestra base.
8. Enviar fechas `yyyy-MM-dd`, horas de 24 horas y números independientes de la cultura de Windows.
9. No reenviar sin transformación un objeto recibido por GET: hay diferencias de forma y significado, especialmente en clientes y pedidos.
10. Una mezcla enviada con `mixcomponents` reemplaza todo el diseño. Una lista parcial elimina componentes omitidos.
11. Enviar un `ticket_num` estable siempre que el despacho lo tenga. Los tickets sin número no se pueden actualizar ni eliminar por la API.
12. Obtener resultados incrementalmente por `bt_resultid`, con cursor por planta y conciliación diaria por fecha.

## 3. Transporte, autenticación y direccionamiento

Fuente: §§4–5, EN pp. 2–6; §16, pp. 33–34.

Base documentada:

```text
http://<direccion-planta>:8080/rhino_rest_api/v1/json
```

El puerto puede cambiar según instalación. JSON es el formato soportado por esta especificación. Nombres de campos en minúsculas y sensibles a mayúsculas; los campos desconocidos se ignoran, por lo que un error de escritura puede pasar inadvertido.

Autenticación por solicitud:

```text
Authorization: Bearer rk_<64-digitos-hexadecimales-minusculos>
```

También se admite `X-API-Key`. La clave es sensible a mayúsculas y se genera/elimina en `Settings > API Keys` de la planta. Solo se muestra al crearla; la planta almacena su hash. Una clave perdida debe reemplazarse. El proveedor recomienda una clave distinta por programa.

- Permiso de lectura: solo GET. Una escritura con esta clave recibe estado de aplicación 403.
- Permiso de lectura/escritura: GET, PUT, POST y DELETE según recurso.
- Clave ausente, inválida o eliminada: 401.
- Tras 10 fallos desde una dirección, incluyendo solicitudes sin clave, se bloquea esa dirección hasta cumplirse un minuto desde el primer fallo, incluso con una clave correcta. Un éxito no reinicia el conteo. Un proxy comparte el bloqueo entre sus clientes.
- Validación de clave con base ocupada: 503 `Database is busy.`.
- No incluir claves en URLs, bitácoras ni código fuente. Fuera de la red local, el proveedor recomienda HTTPS o Tailscale.

Reglas de rutas:

- Las barras finales son equivalentes en la revisión actual. Plantas antiguas pueden responder vacío o 400 `Unsupported format` con barra final; considerar rutas sin ella por compatibilidad.
- Los parámetros de consulta se aceptan, pero ningún recurso los utiliza. No inventar filtros ni paginación con `?page=`.
- Codificar caracteres especiales: `+` debe ser `%2B`; un `+` literal se interpreta como espacio.
- Un código con `/` no puede ir en la URL, ni siquiera como `%2F`. En escrituras, omitir el código de la ruta y enviarlo en el cuerpo.
- Un código con forma `NNNN-NN-NN` se interpreta como fecha. Evitar números de pedido con esa forma.
- Fechas de URL: `YYYY-MM-DD`; rangos inclusivos de días completos. Inicio posterior a fin: 400. Pedidos: máximo 93 días por rango.
- Bajo `loads` y `batchresults`, una selección por ticket exige entero mayor que cero.

## 4. Respuestas y tratamiento de errores

Fuente: §6, EN pp. 6–9.

```json
{
  "request_status": {
    "status": 400,
    "msg": "BAD REQUEST",
    "detail": "Unknown mix_code."
  },
  "error_detail": "Order 445676: mix 3000XX not found."
}
```

La API responde con HTTP 200 y coloca el resultado real en ese bloque. Una URL fuera de la forma de la API puede producir una página HTTP 404 del servidor web. El cliente también debe distinguir errores de transporte, JSON inválido y respuestas sin el bloque esperado.

| Estado de aplicación | Significado |
|---|---|
| 200 | GET con registros o DELETE exitoso; `msg: OK`, `detail: Ok` |
| 201 | Alta: `CREATED` / `Created`; actualización: `UPDATED` / `Updated`. Ambos usan 201 |
| 204 | Consulta sin resultados; `Database query did not return any results.`. Es un código dentro del JSON, no la ausencia de cuerpo HTTP |
| 400 | Validación, JSON, ruta/cuerpo inconsistentes, cliente o mezcla desconocidos en pedidos, formato no soportado |
| 401 | Autenticación; `No log in.`; consultar `error_detail` |
| 403 | Clave de solo lectura (`Forbidden`) o planta incorrecta (`plant_code is not this plant.`) |
| 404 | Registro/recurso inexistente; depende de la operación |
| 405 | DELETE de un recurso distinto de tickets |
| 409 | POST duplicado, conflicto de estado/código/tipo o escritura rechazada por la base |
| 501 | Otro método HTTP no implementado |
| 503 | Base ocupada: `Database is busy.`; admite reintento |

Detalles relevantes: `JSON Error`, `API URL inconsistant with body` (ortografía literal), `Unknown cust_code.`, `Unknown mix_code.`, `Record not found.`, `Unsupported resource.`, `Http POST - Record already exists.`, `Order already has loads.`, `Conflicts with what is stored.`, `Database Error.`.

`detail` discrimina resultados con el mismo estado. `error_detail` explica el registro y campo rechazados; mostrarlo/registrarlo, **no controlar el flujo comparando su redacción**. Los mensajes son en inglés.

Propuesta de cliente: devolver un resultado tipado con HTTP status, estado de aplicación, `msg`, `detail`, `error_detail` y datos. No usar únicamente `IsSuccessStatusCode` ni buscar una propiedad genérica `Message`. No tratar todos los 409 como errores transitorios.

## 5. Contrato general de escritura

Fuente: §7, EN pp. 9–10.

```json
{
  "orders": [
    { "order": { "order_num": "445676", "quantity": 14 } }
  ]
}
```

- Una URL con código admite exactamente un registro. Sin código se admite un lote, sujeto a las restricciones específicas.
- Todos los registros se validan antes de escribir; si uno falla no se guarda ninguno. `plants` es la excepción.
- Campo omitido: se conserva en una actualización; en alta toma el valor predeterminado.
- Texto `""`: vacía el campo; códigos vacíos cuentan como omitidos. `wanted_date: ""` también cuenta como omitido.
- En campos numéricos se aceptan números o cadenas numéricas; `true/false` equivalen a 1/0; `null` y `""` cuentan como omitidos. No generalizar esta regla de `null` a todo campo de texto sin verificar.
- Enteros admiten `5.0`, pero no `5.5`. Las respuestas pueden representar números como cadenas.
- Fechas de cuerpo: años 2000–2099; se aceptan ISO y formatos mes-día-año. Para SaeSync usar siempre ISO: `05-03-2026` sería 3 de mayo, no 5 de marzo.
- Horas: `H:MM`, `HH:MM`, `HH:MM:SS`, almacenadas al minuto. No se admiten `8:00 PM` ni `8:5`.
- Los textos largos se recortan, salvo códigos identificadores como `order_num`, `mix_code`, `mc_code`, `truck_num`, `driver_name`, que se rechazan. Validar las longitudes antes de enviar.
- En clientes, materiales, mezclas y plantas, el código de URL puede seleccionar un registro a renombrar mediante el código del cuerpo. En pedidos, estados y tickets, los códigos deben coincidir; no asumir renombrado universal.

**Diferencia documental:** ES §7 p. 11 dice que los límites se cuentan en bytes y que una letra acentuada/ñ ocupa dos. EN §7 habla de longitud de texto/caracteres y no explicita bytes. Conservar esta observación y confirmar con el proveedor el límite real y la codificación antes de definir truncamientos. No cortar una secuencia UTF-8 ni eliminar acentos por defecto.

## 6. Inventario de endpoints

Todas las rutas son relativas a la base. Las variantes `...list` usan `listitem` y reducen campos; no son filtros adicionales.

| Recurso/ruta | Métodos útiles | Función |
|---|---|---|
| `status`, `status/<result_id>` | GET | Identidad, reloj, versiones, cursor remoto y pendientes |
| `plants`, `plants/<code>` | GET, PUT, POST | Registros de planta; reglas especiales de §7 de este archivo |
| `customers`, `customers/<cust_code>` | GET, PUT, POST | Clientes |
| `customerlist` | GET | Código y nombre de clientes |
| `materials`, `materials/<mat_code>` | GET, PUT | Materiales existentes; POST nunca tiene éxito |
| `materiallist` | GET | Código, nombre y tipo |
| `mixdesigns`, `mixdesigns/<mix_code>` | GET, PUT, POST | Diseños de mezcla |
| `mixdesignlist` | GET | Código y nombre de mezclas |
| `orders` | GET, PUT, POST | GET abiertos; escritura de pedidos |
| `orders/<order_num>` | GET, PUT, POST | Pedido individual |
| `orders/<date>`, `orders/<from>/<to>` | GET | Pedidos solicitados para esos días, cualquier estado |
| `orderslist/...` | GET | Misma selección de pedidos, forma corta |
| `orderstatus/<order_num>` | GET, PUT | Consulta/cambio de estado; PUT también admite ruta sin código |
| `orderloads/<order_num>`, `orderloadslist/<order_num>` | GET | Pedido con todas sus cargas |
| `tickets`, `tickets/<ticket_num>` | PUT, POST | Crear/actualizar carga lista para dosificar |
| `tickets/<ticket_num>` | DELETE | Quitar ticket que no ha comenzado; sin cuerpo |
| `loads`, `loads/<date>`, `loads/<from>/<to>`, `loads/<ticket>` | GET | Cargas; `loadslist/...` es la forma corta |
| `batchresults`, `batchresults/<date>`, `batchresults/<from>/<to>` | GET | Resultados; sin filtro puede devolver mucho historial |
| `batchresults/<mix_code>/<from>/<to>` | GET | Resultados de una mezcla por fechas |
| `batchresults/<ticket>` | GET | Resultado de la carga más reciente con ese ticket |
| `batchresults/since/<result_id>` | GET | Resultados terminados posteriores al cursor, hasta 100 |
| `batchresultlist/...` | GET | Forma corta; también admite `since/<result_id>` |

No hay GET `tickets` documentado. Para comprobar cargas, usar `loads` u `orderloads`. No se soportan creación de materiales, proyectos, búsqueda de clientes, DELETE de otros recursos ni tiempos de entrega de camiones en resultados.

## 7. Plantas y clientes

Fuente: §§8–9, EN pp. 10–13.

### Plantas

`plant_code`: obligatorio, máximo 7; comparación sin distinguir mayúsculas. `plant_name`: máximo 47. `plant_address`: máximo 95.

- GET `plants`: `plants: [{plant: {...}}]`, excluye eliminadas.
- GET `plants/<code>`: objeto `plant` de la computadora consultada, **sin importar el código solicitado**. No usar esa ruta para validar otra planta.
- PUT/POST sin código agrega; PUT actualiza si ya existe; POST existente: 409.
- Escritura con código de URL desconocido: 404 `Unsupported resource.`.
- Campos omitidos se vacían, excepto el código en la actualización descrita.
- Lotes sin transacción: el primer fallo detiene la operación, pero conserva escrituras previas.
- Renombrar la planta propia cambia la identidad contra la que se validan posteriores solicitudes.

### Clientes

| Campo | Regla |
|---|---|
| `cust_code` | Hasta 15; obligatorio, puede venir de URL; comparación sin distinguir mayúsculas |
| `cust_name` | Hasta 64; obligatorio al crear; nombres duplicados se comparan con mayúsculas/minúsculas y causan 409 |
| `cust_first_name`, `cust_last_name` | Se unen en un contacto de hasta 64; enviar cualquiera reemplaza todo el contacto por lo enviado |
| `cust_address` | Dirección de facturación, hasta 95 |
| `cust_bill_to` | Solo escritura; prevalece sobre `cust_address` si se envían ambos |
| `cust_phone`, `cust_mobile`, `cust_fax` | Hasta 31 cada uno |
| `cust_email` | Hasta 63 |
| `cust_udf1` | 1 crédito, 2 COD/pago contra entrega, 3 mal crédito; otros: 400 |
| `cust_udf2` | GET devuelve descuento; PUT escribe nivel de precio 1–3. **No enviarlo hasta resolver esta incompatibilidad** |
| `cust_plantcode` | Opcional; si se envía debe corresponder a la planta destino |
| `cust_id` | Opcional para el identificador de un cliente nuevo; no asumir equivalencia con nuestro ID local |

Alta predeterminada: COD, nivel 1, sin descuento ni exención fiscal. La API no permite fijar descuento/exención. GET `cust_first_name` contiene el contacto completo; no devuelve `cust_last_name`.

GET lista: `customers: [{customer: {...}}]`, orden por crédito y código. Individual: `customer: {...}`. Lista corta: `customerlist: [{listitem: {cust_code, cust_name}}]`.

Cliente eliminado en planta: no se devuelve por GET y su código no se puede recrear por PUT/POST (409). Duplicado en un lote: 400. Renombrar a un código ocupado: 409; cambiar solo mayúsculas del código se ignora. Si el cliente de URL no existe, el código del cuerpo debe coincidir con URL.

## 8. Materiales, mezclas y unidades

Fuente: §§10–11.1, EN pp. 13–19.

### Materiales

Se crean en planta, ligados a tolvas y cableado. PUT desconocido: 404; POST conocido: 409; POST desconocido: 404. `mat_code` distingue mayúsculas y tiene máximo 19.

Campos modificables: `mat_code` (renombrado vía URL), `mat_plantcode` (validación de destino), `mat_type` (misma familia), `mat_source` (hasta 9), `mat_spgr` (>0 y <=10), `mat_absorption` y `mat_totalmoisture` (solo agregados), `mat_dosagetype` (solo aditivos).

`mat_name`, `mat_ssd`, `mat_order`, `mat_price` se ignoran al escribir. No cambia configuración de dosificación, costo, descripción, inventario ni secuencia/retardo de aditivos. GET `mat_name` equivale a `mat_code`; `mat_order` es 0; `mat_ssd` de agregados es `"1"`.

| Tipo | Familia | Código que devuelve GET |
|---|---|---|
| 1, 2, 3 | Agregado grueso, fino, cualquiera | 3 |
| 4, 5, 6 | Cemento, sustituto, cualquier cementante | 6 |
| 7 | Aditivo | 7 |
| 8 | Agua | 8 |

Cambiar de familia o enviar tipo desconocido produce 409. `mat_dosagetype`: 0 por yd³/m³; 1 o 2 por hundredweight (100 lb) de cemento, ambos se guardan/devuelven como 1; 3 por carga se rechaza (400).

GET lista: `materials: [{material: {...}}]`; individual: `material: {...}`; lista corta: `materiallist: [{listitem: {mat_code, mat_name, mat_type}}]`.

### Mezclas

Forma de escritura: `mixdesigns: [{mixdesign: {mixinfo: {...}, mixcomponents: [{mixcomponent: {...}}]}}]`. GET individual usa un objeto `mixdesign`; GET lista conserva la estructura anterior. Lista corta: `mixdesignlist: [{listitem: {mix_code, mix_name}}]`.

| `mixinfo` | Regla/default de alta |
|---|---|
| `mix_code` | Hasta 19; obligatorio o tomado de URL; comparación sin distinguir mayúsculas |
| `mix_name` | Obligatorio al crear, hasta 63 |
| `mix_unittype` | 0 US/yd³ (default), 1 métrico/m³ |
| `mix_price` | >=0, default 0 |
| `mix_slump` | >=0, default 4 |
| `mix_maxsize` | >0, default 10 |
| `mix_mixtime` | Minutos, >=0, default 0 |
| `mix_air` | >=0, default 0 |
| `mix_ssd` | 0 seco al horno, 1 SSD/SSS (default) |
| `mix_yield` | >=0, default 1 |
| `mix_notes` | Hasta 63 |
| `mix_plantcode` | Opcional, debe ser la planta destino |

Componentes: `mc_code` obligatorio, hasta 19, material existente con escritura exacta; `mc_quantity` obligatorio >=0, cantidad por yd³ o m³. Opcionales: `mc_unit`, `mc_order` >=0 (default posición en lista), `mc_type` (familia correcta), `mc_mixcode` (código de mezcla anterior o nuevo). `mc_name`, `mc_source`, `mc_spgr`, `mc_absorption` se leen pero se ignoran al escribir mezclas; modificarlos mediante materiales.

- Alta necesita nombre y entre 1 y 24 componentes; cada material una vez.
- Omitir `mixcomponents` actualiza solo `mixinfo`. Enviarlo reemplaza **todo** el diseño. `[]` es inválido.
- Cambiar `mix_unittype` requiere componentes en la misma solicitud con cantidades en las nuevas unidades.
- `mc_unit` debe coincidir con la unidad usada por la planta para ese material. No realiza una conversión implícita solicitada por el cliente.
- Material inexistente: 404; tipo de componente incorrecto: 409; unidad incompatible: 400.
- Mezcla actualmente en dosificación: 409; se puede intentar cuando termine la carga. Las cargas en cola permanecen.
- Mezcla eliminada o código ocupado por otro producto: 409. Mezcla repetida en lote: 400.
- Renombrar conserva pedidos/cargas asociados. Cambio solo de mayúsculas sí se guarda, a diferencia de clientes.
- Código distinto entre URL y cuerpo solo puede renombrar una mezcla existente; al crear deben coincidir.

### Códigos de unidad

| Código | Unidad | Código | Unidad |
|---|---|---|---|
| 0 | Ninguna/otra | 2 | Pies |
| 6 | Libras | 7 | Toneladas cortas |
| 10 | Onzas líquidas | 11 | Galones |
| 16 | Yardas cúbicas | 26 | Horas |
| 106 | Kilogramos | 107 | Toneladas métricas |
| 110 | Mililitros | 111 | Litros |
| 116 | Metros cúbicos | | |

Componentes US: agregados/cemento/agua pesada en 6, agua medida en 11, aditivos en 10; métricos: 106, 111, 110 respectivamente. No asumir que toda planta o mezcla trabaja en m³ por estar en México.

## 9. Pedidos y estados

Fuente: §§12–13, EN pp. 19–24.

`order_num` es el identificador del despacho, hasta 31. La API no exige que sea nuestro `IDPedido`; ese mapeo debe acordarse. PUT crea o actualiza; POST existente: 409. URL y cuerpo deben coincidir.

| Campo de escritura | Alta/regla |
|---|---|
| `order_num` | Obligatorio, puede venir de URL |
| `cust_code` | Obligatorio, cliente existente/no eliminado; desconocido: 400 |
| `mix_code` | Obligatorio, mezcla existente/no eliminada; desconocida: 400 |
| `quantity` | Obligatorio >0 |
| `status_id` | Opcional, default 1; alta no permite 6 ni 7 |
| `wanted_date` | Fecha, opcionalmente con hora; vacío no la borra |
| `wanted_time` | Solo fecha implica 06:00; hora sola conserva fecha existente y se ignora en alta sin fecha |
| `load_size` | Si se envía >0; omitirlo en alta deja 0. Se recomienda enviarlo |
| `num_loads` | >0; no se almacena; ajusta extras por carga al cambiar cantidad |
| `delivery_addrs` | Dirección de entrega, hasta 95; este es el nombre exacto |
| `po_num` | Orden de compra, hasta 31 |
| `plant_code` | Opcional, validado contra el destino |
| `slump` | Revenimiento objetivo, default 3 |
| `notes` | Instrucciones, hasta 255 |
| `mix_price` | >=0; precio propio del pedido. Si se omite en alta usa catálogo de mezcla |

Al cambiar `quantity`, los extras por volumen la siguen; los extras por carga siguen `num_loads`, o `ceil(quantity / load_size)` si se omite. Traslado default 15 minutos y espaciamiento 30, valores fijos modificables en pantalla de planta. Pedidos nuevos toman región fiscal 2 independientemente del cliente: confirmar adecuación con el proveedor/negocio.

| Estado de pedido | Significado |
|---|---|
| 1 | Send (nuevo) |
| 2 | Will call (cliente avisará) |
| 3 | Weather (espera por clima) |
| 4 | Loading |
| 5 | On hold |
| 6 | Complete |
| 7 | Canceled |

Cambiar mezcla solo se permite sin cargas y en estado 1–3; de otro modo 409 `Order already has loads.`. Pasar a Complete/Canceled elimina cargas no iniciadas y estaciona las iniciadas; cancelar devuelve extras inventariados al inventario. No tratar un cambio de estado como una mera etiqueta.

Lecturas:

- `orders`: abiertos 1–5; ordenados por hora solicitada menos traslado, sin fecha primero.
- Por fecha/rango: fecha **solicitada**, cualquier estado; máximo 93 días.
- Individual: objeto `order`. `orderslist`: `listitem` con `order_num`, `status_id`, `qty_required`, `qty_remaining`.
- `qty_remaining` es lo todavía no programado como carga; no equivale a volumen pendiente de producir/entregar. Lo ya en cola cuenta como programado.
- GET devuelve `customer` (nombre), no `cust_code`; `qty_required`, no `quantity`; `price`, no `mix_price`. Separar DTO de entrada/salida.
- `wanted_date` se devuelve como `YYYY-MM-DD HH:MM` o vacío.
- `price` es el precio propio de la mezcla en ese pedido antes del descuento. `mix_time` está en **segundos**, frente a `mix_mixtime` en minutos.
- Otros campos GET: contacto/teléfonos/descuento, `mix_id`, `mix_code`, `description`, `slump`, `metric`, `travel_time`, `spacing`, `po_num`, `delivery_addrs`, `notes`, `last_load_start` y opciones de planta (`return_allowed`, `cash`, `backtoback`, `plus`, `estimate`, `project_id`, `use_cert`, `use_report`). Su presencia no implica que sean escribibles.

Estado: PUT `orderstatus` usa **`{"orderstatus":[{"order":{"order_num":"445676","status_id":6}}]}`**. Solo modifica estado, valida planta si se envía e ignora otros campos. Pedido desconocido: 404; POST: 409; éxito: 201 Updated.

GET `orderstatus/<order_num>` devuelve `orderstatus: [{order: {...}}]` con los campos completos del pedido; desconocido: 204 y arreglo vacío; GET sin número: 400.

GET `orderloads/<order_num>` devuelve `order: {..., loads: [{load: {...}}]}`. Incluye todas las cargas del pedido, sin pesos de componentes; sin cargas: `[]`; pedido inexistente: 204; sin número: 400. La forma `orderloadslist` sustituye cada envoltorio `load` por `listitem`.

## 10. Tickets: una carga lista para dosificar

Fuente: §14, EN pp. 24–29.

Un ticket encola una carga en estado **Ready (3)**; el operador de planta inicia la dosificación. La misma llamada puede crear pedido, camión y operador, pero requiere que cliente, mezcla y productos extra ya existan.

| Campo | Regla |
|---|---|
| `ticket_num` | Entero >0, opcional o tomado de URL; enviar un número estable siempre que exista |
| `order_num` | Obligatorio también en actualización; hasta 31; debe concordar con `order.order_num` si se envía |
| `load_size` | Obligatorio al crear, >0; puede actualizarse antes del inicio |
| `truck_num` | Obligatorio al crear, hasta 15; camión inexistente se crea |
| `driver_name` | Opcional, hasta 15; operador inexistente se crea |
| `message` | Hasta 255, texto más largo se recorta |
| `order` | Campos de PUT orders; obligatorio para crear el pedido, con `cust_code`, `mix_code`, `quantity` |
| `extras` | Hasta 13, códigos únicos; reemplaza extras del ticket; `[]` los quita |
| `totals` | Reemplaza totales del ticket; `subtotal`, `tax`, `cumulative_total`, `grand_total` >=0; cero no se imprime |

Sin `ticket_num`, Rhino asigna el número al imprimir, responde `ticket_num: 0` y la API no permite modificar/eliminar ese ticket. No usar ese modo para reintentos automáticos.

Extras:

- `code`: producto existente, no mezcla, comparación sin distinguir mayúsculas. No encontrado/eliminado: 404; mezcla: 400.
- `load_qty`: cantidad de este ticket >=0.
- `order_qty`: acumulado de todo el pedido >=0; **reemplaza** la cantidad facturable del producto, no la incrementa. Omitirlo conserva la facturación.
- `price`: precio unitario del ticket; omitirlo usa catálogo para el ticket. Si se envía también cambia el precio de la línea facturable del pedido; si se omite, una línea facturable existente conserva su precio y una nueva toma catálogo.
- `amount`: importe >=0; default `load_qty * price`.
- `description`, `unit`: impresión; descripción omitida usa la del producto. Impresión limita código a 7, descripción a 15, unidad a 3; facturación conserva el producto completo.

Respuesta: `tickets: [{ticket: {ticket_num, load_id, order_num, result, added_at_plant}}]`; `result` es `created` o `updated`. Estado global 201 Created si se creó algún ticket, de otro modo 201 Updated. Revisar `added_at_plant`: un error en número de camión/nombre de operador puede crear registros nuevos.

Reglas de actualización y conflicto:

- PUT del mismo ticket lo modifica solo mientras no haya empezado ni esté cancelado. Iniciado/cancelado: 409 y no guarda nada.
- Campos omitidos se conservan, incluidos `extras` y `totals`; la mezcla de la carga permanece aunque se haya cambiado en la pantalla de planta.
- Un número usado en otro pedido puede reutilizarse si la carga anterior terminó; si sigue abierta: 409. **Ticket no es una clave global eterna del historial.**
- Un ticket para pedido Complete lo reabre a Loading (4). Para Canceled: 409 salvo que el bloque `order` envíe un nuevo `status_id` permitido.
- El bloque `order` del ticket no puede fijar Complete ni Canceled (400); usar `orderstatus`.
- No puede cambiar la mezcla de un pedido que ya tiene cargas (409).
- No enviar dos tickets del mismo pedido ni repetir ticket en una sola solicitud (400). Enviarlos en solicitudes separadas.
- Camión/operador se comparan sin distinguir mayúsculas. `driver_name` cambia el operador de la carga y del camión. Si se omite y se mueve a otro camión, toma su operador; con el mismo camión conserva el operador.
- Camión nuevo recibe capacidad 12 o el tamaño de carga si es mayor. Operador por prioridad: ticket, carga, primer operador de planta. Si no existe ninguno, crea `Unassigned`. Camión eliminado: 409.
- Lote vacío/sin `tickets`, negativos, campos inválidos, extras duplicados o >13: 400.
- POST de ticket existente: 409; pedido inexistente sin bloque `order`: 404. Los tickets se validan en orden y se devuelve el primer error encontrado.

DELETE `tickets/<ticket_num>` no lleva cuerpo. Éxito: estado 200 y objeto `ticket` con `result: deleted`. Iniciado/cancelado: 409; desconocido: 404. No elimina pedido ni ajusta extras facturables. Enviar el `order_qty` corregido con el siguiente ticket; un pedido sin tickets sigue abierto hasta cancelarlo por estado.

## 11. Cargas, resultados y estructura JSON

Fuente: §§15.1–15.2, EN pp. 29–32.

Estados de carga, distintos de estados de pedido:

| Valor | Carga |
|---|---|
| 1 | Initialized |
| 2 | Pending |
| 3 | Ready |
| 4 | Started |
| 5 | Complete |
| 6 | Canceled |

GET `loads` devuelve no canceladas, antiguas primero; en esa consulta sin filtros `order_num` es vacío. Fechas seleccionan cargas hechas esos días, no canceladas. Por ticket devuelve la carga más reciente con ese número, en cualquier estado, en arreglo de un elemento. Una carga abortada y terminada manualmente cuenta como Complete.

**Envoltorio especial de `loads`** (no confundir con `orderloads`):

```json
{
  "request_status": { "status": 200, "msg": "OK", "detail": "Ok" },
  "loads": [
    {
      "loads": {
        "load": { "ticket": 100050, "order_num": "445676", "status": 5 },
        "batchcomponents": [
          { "batchcomponent": { "bc_matcode": "Cement", "bc_actual": 1000, "bc_unit": 106 } }
        ]
      }
    }
  ]
}
```

Ejemplo estructural abreviado; no enumera todos los campos que devuelve la planta.

Campos `load`: `plantcode`, `order_num`, `ticket`, `status`, `load_date`, `load_time`, `load_size`, `mix_code`, `mix_id`, `mix_descr`, `truck_id`, `driver_id`, `driver_name`, `driver_phone`, `deliv_stat_id`, `deliv_stat`, `batches`, `hundred_wts`, `water_in_truck`, `water_adjust`, `water_trim`, `message`, `last`, `returned_qty`, `customer_name`, `batch_man`. Fechas `YYYY-MM-DD`, horas `HH:MM:SS`; `last=1` indica última carga. `hundred_wts` expresa cientos de libras de cemento.

`loadslist` usa `listitem` con `plantcode`, `order_num`, `ticket`, `status`, `load_date`, `load_time`, `load_size`, `mix_code`, sin pesos. Las cargas siguen devolviéndose si se borraron registros relacionados, con nombres vacíos.

Forma de resultados: `batchresults: [{batchresult: {batchinfo: {...}, batchcomponents: [{batchcomponent: {...}}]}}]`. Por ticket, GET `batchresults/<ticket>` devuelve **objeto singular `batchresult`**; su variante corta por ticket devuelve arreglo de un elemento.

| Campo de `batchinfo` | Significado |
|---|---|
| `bt_ticket` | Número de ticket |
| `bt_plantcode` | Planta |
| `bt_mixcode` | Código de mezcla |
| `bt_custcode` | ID numérico interno del cliente en Rhino, **no `cust_code`**; 0 si fue eliminado |
| `bt_jobcode` | Número de pedido |
| `bt_loaddate`, `bt_loadtime` | Fecha/hora |
| `bt_loadsize` | Tamaño de carga |
| `bt_waterintruck`, `bt_watertrim` | Agua en camión y ajuste de agua de carga |
| `bt_batcher`, `bt_truck`, `bt_driver` | Dosificador, número de camión, operador |
| `bt_resultid` | Identificador incremental del resultado terminado; 0 si no terminó |

Campos `batchcomponent`: `bc_ticket`, `bc_matcode`, `bc_mattype`, `bc_design` (cantidad de diseño), `bc_target` (objetivo para carga), `bc_actual` (dosificado), `bc_unit`, `bc_freemoisture` (% humedad libre). Conservar unidades y distinguir objetivo de real.

Por fecha, resultados incluyen cargas todavía no dosificadas (`bt_resultid=0`) y excluyen canceladas. `batchresultlist` devuelve `listitem` con `bt_ticket`, `bt_loaddate`, `bt_loadtime`, `bt_loadsize`, `bt_mixcode`, `bt_resultid`. No usar `bt_resultid=0` como identificador único de registros pendientes.

## 12. Recolección incremental y conciliación

Fuente: §15.3, EN pp. 32–33.

GET `status` devuelve un objeto `status` con `plant_code`, `plant_name`, `plant_time` (hora local `YYYY-MM-DD HH:MM:SS`), `api_version`, `software_version`, `newest_result_id`. GET `status/<result_id>` agrega `results_pending`.

Contrato del proveedor:

1. Conservar el mayor `bt_resultid` recibido. Empezar en 0 recupera todo el historial, que puede ser grande.
2. Consultar `status/<cursor>` con la frecuencia necesaria.
3. Si hay pendientes, pedir `batchresults/since/<cursor>`: solo terminados posteriores al ID, en orden ascendente, máximo 100 por respuesta.
4. Guardar resultados y último ID. Repetir mientras `more` sea 1. `more` siempre se envía y 1 significa página llena; la consulta siguiente puede estar vacía.
5. Sin nuevos resultados: estado de aplicación 204 con arreglo vacío.
6. Una vez al día, consultar también por fecha. Una carga completada manualmente tras una interrupción conserva el ID de su última bachada y puede no aparecer como nueva para el cursor actual.

La planta no registra qué leyó cada consumidor. Varios lectores tienen cursores propios y no afectan ULink. Se incluyen resultados cuyos registros relacionados fueron eliminados.

Propuesta SaeSync (no implementación existente): persistir resultados y avance de cursor en una misma transacción local; avanzar solo después de guardar correctamente. Deduplicar resultados terminados por identidad de planta y `bt_resultid`, permitiendo actualizar datos durante conciliación. No confiar solo en ticket, dado que se reutiliza. Mantener por separado la clave de pendientes (`bt_resultid=0`), que requiere definición. Detectar retroceso del cursor remoto por posible restauración/cambio de equipo y solicitar conciliación; el comportamiento ante restauraciones no está especificado.

## 13. Ejemplos mínimos para implementar

Son ejemplos sintéticos; los códigos deben existir o crearse conforme a las reglas. Agregar encabezado de autenticación y `Content-Type: application/json`. No incluyen claves reales.

PUT `customers/CLI001`:

```json
{
  "customers": [
    { "customer": { "cust_code": "CLI001", "cust_name": "Cliente de prueba", "cust_udf1": 1 } }
  ]
}
```

PUT `orders/PED-1001`, con cliente y mezcla ya disponibles:

```json
{
  "orders": [
    {
      "order": {
        "order_num": "PED-1001",
        "cust_code": "CLI001",
        "mix_code": "MIX001",
        "quantity": 14,
        "load_size": 7,
        "num_loads": 2,
        "wanted_date": "2026-10-23",
        "wanted_time": "13:30",
        "delivery_addrs": "Direccion de la obra",
        "status_id": 1
      }
    }
  ]
}
```

PUT `tickets/100050`, con pedido ya disponible:

```json
{
  "tickets": [
    {
      "ticket": {
        "ticket_num": 100050,
        "order_num": "PED-1001",
        "load_size": 7,
        "truck_num": "CR12",
        "driver_name": "OPERADOR01",
        "message": "Acceso por puerta 2"
      }
    }
  ]
}
```

PUT `orderstatus/PED-1001` para cierre, considerando efectos sobre cargas:

```json
{
  "orderstatus": [
    { "order": { "order_num": "PED-1001", "status_id": 6 } }
  ]
}
```

## 14. Relación con el proyecto actual

Estas observaciones provienen del repositorio, no del contrato del proveedor. La ruta real encontrada es `SaeSync/SaeSync.ClienteCB3/Documentos`.

### Estado del cliente observado

- [SaeSync.ClienteCB3.csproj](../SaeSync.ClienteCB3.csproj): Windows Forms, .NET Framework 4.6.2, Newtonsoft.Json 13.0.3 y System.Net.Http.
- [ConfigReader.cs](../ConfigReader.cs): claves de configuración `UrlPutCustomers`, `UrlPutOrders`, `UrlGetOrders`; los nombres no determinan el método HTTP realmente usado.
- [Form1.cs](../Form1.cs): reutiliza HttpClient y establece timeout de 30 segundos. Los flujos revisados envían POST, evalúan `IsSuccessStatusCode` y consultan `apiResponse?.Message`; deben adaptarse al contrato de estado interno.
- En la construcción del HttpClient revisada solo se configura `Accept`; no se observó envío de API key en estos flujos.
- `ACCION_PEDIDOS` toma la URI de `txtURL` (usado para clientes), aunque existe `txtUrlPedidos`.
- El JSON de pedidos escribe `delivey_addrs`, pero el contrato requiere **`delivery_addrs`**. El proveedor ignora nombres desconocidos.
- El envío de pedidos tiene `mix_code` fijo; las lecturas de clientes/pedidos usan `MTY` fijo. No representan aún un enrutamiento configurable por planta.
- La limpieza actual mediante regex de caracteres URI no sustituye validaciones de texto JSON, codificación de segmentos URL ni límites de campo.

No se modificó este código en el análisis. No asumir que ya cumple la revisión 2026-10-20.

### Extracción de pedidos existente

[CARGA_ORDENES_CB3.sql](../../SaeSync.DB/dbo/Stored%20Procedures/CARGA_ORDENES_CB3.sql) vacía `SyncCB3.dbo.Orders` y la repuebla con pedidos del día/planta `M4`, mezcla fija, dirección del cliente y tamaño de carga calculado. Debe revisarse antes de reutilizarlo:

- `Viajes=0` produce `load_size=0`, que se rechaza si se envía; `num_loads=0` también es inválido.
- Mezcla fija y filtro fijo de planta impiden generalizar el proceso.
- Confirmar que la dirección requerida sea la obra y no la dirección de facturación del cliente.
- El esquema de la base externa `SyncCB3` no queda definido por ese procedimiento; no asumir que SaeSync.DB contiene todas sus tablas.
- El vaciado global no constituye una cola durable con confirmaciones, reintentos o control de concurrencia.

### Mapeos candidatos, pendientes de validación

| Concepto Rhino | Fuente local candidata | Decisión pendiente |
|---|---|---|
| `plant_code` y destino API | `Plantas` / `CvePlanta` | Correspondencia exacta, URL, clave y zona horaria por planta |
| `cust_code` | `Clientes.ClaveCliente` y ciudad | No confundir con `Clientes.IDCliente`; definir alcance y preservación de ceros |
| `order_num` | `Pedidos.IDPedido` / `PedidoDetalle.IDPedido` | Cabecera frente a pedido hijo y unicidad por destino |
| `mix_code` | Producto asociado al pedido | Equivalencia con mezcla Rhino existente, no un literal fijo |
| `quantity`, `load_size` | `PedidoProducto.Cantidad`, programación/viajes | Total frente a carga individual, unidades y redondeos |
| `delivery_addrs` | `Obra` | Construcción y límite 95 |
| `ticket_num` | Remisión/viaje/cierre | Nuestra remisión puede ser texto; Rhino exige entero >0. Definir correlación reversible |
| `truck_num`, `driver_name` | `Unidad`, `Personal` | Códigos de hasta 15, catálogo por planta y control de altas automáticas |
| Resultados/pesos | Tablas nuevas o integración con CB2/cierres | Destino de componentes, historial, cursor y conciliación |

Las tablas existentes [Sync_Ejecucion](../../SaeSync.DB/dbo/Tables/Sync_Ejecucion.sql) y [Sync_Detalle](../../SaeSync.DB/dbo/Tables/Sync_Detalle.sql) ofrecen bitácora básica. Sus definiciones revisadas no contienen un cursor `bt_resultid` por planta ni el detalle de componentes dosificados. Su reutilización requiere diseño; no mezclar HTTP status con `request_status.status` en un único campo ambiguo.

## 15. Flujo propuesto y criterios de verificación

Esta sección contiene recomendaciones de implementación, no requisitos adicionales de Rhino ni cambios ya hechos.

1. Configurar cada planta, credencial protegida y versión instalada. Consultar `status` y comparar la identidad con la configuración antes de escribir.
2. Leer catálogos necesarios. Acordar qué sistema gobierna clientes/mezclas; no sobrescribir diseños o configuración de planta automáticamente.
3. Preparar clientes, verificar mezclas y enviar pedidos con PUT cuando el propósito sea sincronizar altas/cambios. Mantener POST para operaciones deliberadamente solo de alta.
4. Enviar tickets con correlación durable. Serializar operaciones de un mismo pedido, especialmente acumulados `order_qty`.
5. Registrar petición lógica, planta, operación, claves de negocio, intento, tiempos, HTTP status, estado API y error; ocultar credenciales y limitar datos personales en logs.
6. Ante timeout, el resultado de una escritura es **desconocido**: consultar el estado remoto antes de reenviar. No convertir automáticamente un 409 en éxito ni asumir que PUT es incondicionalmente repetible después de cambios operativos.
7. Reintentar 503 con espera creciente y límite. Detener reintentos de autenticación/configuración; tratar conflictos según recurso. No hay intervalo de polling, límite general de tasa ni SLA documentados.
8. Recolectar resultados con cursor durable y ejecutar conciliación diaria por fecha local de planta. Reprocesar de forma idempotente los registros ya recibidos.

Casos mínimos a verificar al desarrollar:

- HTTP 200 con estado API 400/401/403/409/503 se reconoce como fallo; 201 Updated se reconoce como éxito; 204 se interpreta desde JSON.
- Campos numéricos en cadena y número; fechas ISO y hora sin cultura local; respuestas vacías/HTML se distinguen de éxito.
- DTO de cada envoltorio: singular/lista, `loads` doble, `orderstatus` con `order`, `orderloads`, `batchresults` singular.
- Un registro inválido revierte todo un lote normal; prueba separada para comportamiento parcial de plantas.
- Actualizaciones omiten valores no modificados; `cust_udf2` no se reenvía desde GET.
- Validación de límites, acentos y códigos; nombres de campo exactos; no enviar códigos de planta de otro destino.
- Ticket repetido, iniciado, cancelado, sin número y reutilizado en otro pedido; errores de altas automáticas de camión/operador.
- Dos tickets del mismo pedido se envían separados; extras acumulados reemplazan y no suman.
- Reinicio del consumidor entre guardar resultados y cursor; página de 100 con `more=1`, seguida de 204; conciliación de cierre manual con ID antiguo.
- Validar unidades de mezcla/componentes y conversión minutos/segundos antes de comparar datos.

## 16. Pendientes de decisión y compatibilidad

No bloquean este documento, pero deben resolverse para implementar/desplegar:

- Direcciones/puertos, códigos reales, credenciales y permisos por planta; comprobar versión con `status`.
- Tickets requieren actualización posterior al 2026-10-19; comportamiento de esta revisión requiere actualización posterior al 2026-10-20. Plantas antiguas devuelven 404 para tickets. No se documenta un número mínimo de build: no inventarlo a partir del ejemplo `1.4.10.6`.
- Alcance inicial: clientes, pedidos, tickets, mezclas, materiales, lectura de resultados; propiedad de cada dato y resolución de cambios hechos en planta.
- Confirmar bytes frente a caracteres en límites y efecto sobre nombres mexicanos; aclaración específica de ES p. 11.
- Resolver `cust_udf2` con proveedor; omitir mientras tanto.
- Mapeo de IDs locales/ciudad/planta, pedidos hijos, remisiones alfanuméricas y números de ticket reutilizables.
- Sistema de unidades por mezcla/planta; unidad de revenimiento y cantidades de agua no quedan completamente explicitadas para todos los campos.
- Región fiscal 2, precios, descuentos, moneda/precisión y autoridad de extras/totales; no asumir cálculo fiscal equivalente al local.
- Arranque de cursor desde 0 o política acordada de corte; retención de resultados, recuperación ante restauración y conciliación histórica.
- Timeouts, frecuencia, tamaño de lotes de escritura y concurrencia: no hay límites generales documentados salvo los expresos (93 días, 100 resultados, 24 componentes, 13 extras).

## 17. Índice de referencias y mantenimiento

| Tema | Secciones PDF | Páginas EN | Páginas ES |
|---|---|---|---|
| Transporte, autenticación y URL | 4–5 | 2–6 | 3–7 |
| Respuestas y reglas generales | 6–7 | 6–10 | 7–11 |
| Plantas/clientes | 8–9 | 10–13 | 12–15 |
| Materiales y mezclas | 10–11.1 | 13–19 | 15–23 |
| Pedidos, estados y cargas | 12–13 | 19–24 | 23–28 |
| Tickets | 14 | 24–29 | 28–34 |
| Cargas/resultados/cursor | 15 | 29–33 | 34–38 |
| Limitaciones/revisiones | 16/Apéndice A | 33–35 | 39–40 |

Al recibir nueva documentación, actualizar fecha/revisión, comparar cambios y conservar la separación entre contrato confirmado, observaciones del código y decisiones pendientes. Al implementar, sustituir las propuestas por enlaces a las clases, migraciones y pruebas reales. Este archivo no demuestra compatibilidad de una instalación concreta sin verificación contra esa planta.
