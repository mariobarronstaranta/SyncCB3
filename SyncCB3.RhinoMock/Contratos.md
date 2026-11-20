# Especificación de Contratos: Concrete Rhino REST API (v1)

> **Documento fuente:** `Concrete_Rhino_REST_API_Specification.pdf` (Automation Logic, LLC - Rev. 2026-10-20)  
> **Proyecto:** `SyncCB3.RhinoMock` (`C:\Concretec\SyncCB3\SyncCB3.RhinoMock\Contratos.md`)  
> **Propósito:** Especificación exhaustiva de contratos de datos (Data Contracts / DTOs), esquemas JSON, encabezados, códigos de error y valores enumerados para la implementación de clientes y mocks.

---

## Índice

1. [Convenciones Globales y Envoltorio Estándar](#1-convenciones-globales-y-envoltorio-estándar)
2. [Encabezados HTTP y Seguridad](#2-encabezados-http-y-seguridad)
3. [Tablas de Códigos y Enumeraciones](#3-tablas-de-códigos-y-enumeraciones)
4. [Contratos por Recurso](#4-contratos-por-recurso)
   * 4.1. [Estado y Ping (`status/`)](#41-estado-y-ping-status)
   * 4.2. [Plantas (`plants/`)](#42-plantas-plants)
   * 4.3. [Clientes (`customers/`, `customerlist/`)](#43-clientes-customers-customerlist)
   * 4.4. [Materiales (`materials/`, `materiallist/`)](#44-materiales-materials-materiallist)
   * 4.5. [Diseños de Mezcla (`mixdesigns/`, `mixdesignlist/`)](#45-diseños-de-mezcla-mixdesigns-mixdesignlist)
   * 4.6. [Órdenes (`orders/`, `orderslist/`, `orderstatus/`, `orderloads/`)](#46-órdenes-orders-orderslist-orderstatus-orderloads)
   * 4.7. [Tickets de Carga (`tickets/`)](#47-tickets-de-carga-tickets)
   * 4.8. [Cargas de Despacho (`loads/`, `loadslist/`)](#48-cargas-de-despacho-loads-loadslist)
   * 4.9. [Resultados de Dosificación (`batchresults/`, `batchresultlist/`)](#49-resultados-de-dosificación-batchresults-batchresultlist)
5. [Matriz de Códigos de Respuesta y Errores](#5-matriz-de-códigos-de-respuesta-y-errores)

---

## 1. Convenciones Globales y Envoltorio Estándar

### 1.1 Regla del Código HTTP de Transporte
* El servidor web responde casi invariablemente con **HTTP 200 OK** a nivel de transporte.
* El resultado de la operación se evalúa dentro del objeto JSON `request_status`.

### 1.2 Envoltorio de Respuesta Base (`request_status`)
Toda respuesta de la API inicia con el bloque `request_status`:

```json
{
  "request_status": {
    "status": 200,
    "msg": "OK",
    "detail": "Ok"
  },
  "error_detail": "Mensaje descriptivo opcional en caso de rechazo"
}
```

#### Esquema del Bloque de Estado
| Campo | Tipo | Obligatorio | Descripción |
| :--- | :--- | :--- | :--- |
| `status` | `int` | Sí | Código de estado estilo HTTP (200, 201, 204, 400, 401, 403, 404, 405, 409, 501, 503). |
| `msg` | `string` | Sí | Nombre canónico del código HTTP (ej. `"OK"`, `"CREATED"`, `"BAD REQUEST"`, `"CONFLICT"`). |
| `detail` | `string` | Sí | Clasificador específico de la causa o resultado (ej. `"Created"`, `"JSON Error"`, `"Unknown cust_code."`). |
| `error_detail` | `string` | Opcional | Texto plano para registro o usuario que indica qué registro y campo fallaron. Solo presente en rechazos. |

### 1.3 Estructura de Peticiones de Escritura (PUT / POST)
Las peticiones que envían colecciones requieren un arreglo con el nombre en plural del recurso, y cada registro envuelto en su nombre singular:
```json
{
  "<recursos>": [
    {
      "<recurso>": { ... campos ... }
    }
  ]
}
```
* Si la URL incluye un identificador (ej. `orders/445676`), el arreglo debe contener **exactamente un elemento**.

---

## 2. Encabezados HTTP y Seguridad

| Encabezado | Valor / Formato | Obligatorio | Notas |
| :--- | :--- | :--- | :--- |
| `Authorization` | `Bearer rk_<64_hex_digits>` | Recomendado | Clave de API generada en la planta. Minúsculas. |
| `X-API-Key` | `rk_<64_hex_digits>` | Alternativo | Aceptado como alternativa al encabezado Authorization. |
| `Content-Type` | `application/json` | En PUT / POST | Mandatorio en cuerpos JSON. |

* **Lockout:** 10 intentos fallidos (o sin token) bloquean la dirección IP del cliente durante 60 segundos.

---

## 3. Tablas de Códigos y Enumeraciones

### 3.1 Familias de Materiales (`mat_type` / `mc_type` / `bc_mattype`)
| Código | Familia / Significado | Retornado por GET en Materiales |
| :---: | :--- | :---: |
| `1` | Agregado grueso (Coarse Aggregate) | - |
| `2` | Agregado fino (Fine Aggregate / Arena) | - |
| `3` | Cualquier Agregado | Retornado para todo agregado |
| `4` | Cemento Portland | - |
| `5` | Cemento alternativo / Ceniza volante (Fly ash / Slag) | - |
| `6` | Cualquier Cemento / Cementante | Retornado para todo cemento/ceniza |
| `7` | Aditivo químico (Admixture) | Retornado para todo aditivo |
| `8` | Agua (Water) | Retornado para agua |

### 3.2 Unidades de Medida (`mc_unit` / `bc_unit`)
| Código | Unidad | Símbolo / Uso habitual |
| :---: | :--- | :--- |
| `0` | Ninguna u otra | `none / other` |
| `2` | Pies | `ft` |
| `6` | Libras (US) | Agregados, cementos, agua pesada (`lbs`) |
| `7` | Toneladas cortas (US) | `short tons` (2,000 lbs) |
| `10` | Onzas fluidas (US) | Aditivos en sistema imperial (`fl oz`) |
| `11` | Galones (US) | Agua medida por contador volumétrico (`gal`) |
| `16` | Yardas cúbicas | Volumen de concreto en sistema US (`yd³`) |
| `26` | Horas | `hrs` |
| `106` | Kilogramos | Agregados, cementos, agua pesada (`kg`) |
| `107` | Toneladas métricas | `t` (1,000 kg) |
| `110` | Mililitros | Aditivos en sistema métrico (`mL`) |
| `111` | Litros | Agua medida volumétricamente en métrico (`L`) |
| `116` | Metros cúbicos | Volumen de concreto en métrico (`m³`) |

### 3.3 Estados de Órdenes (`status_id`)
| ID | Nombre en Planta | Descripción |
| :---: | :--- | :--- |
| `1` | Send (New) | Orden nueva enviada desde despacho. |
| `2` | Will call | Por confirmar / En espera de llamada del cliente. |
| `3` | Weather | En espera por condiciones climáticas. |
| `4` | Loading | En proceso de carga / activa con viajes. |
| `5` | On hold | Detenida temporalmente en planta. |
| `6` | Complete | Finalizada. Cancela viajes en cola no iniciados. |
| `7` | Canceled | Cancelada. Retorna aditivos/extras a inventario. |

### 3.4 Estados de Cargas / Viajes (`status` en `loads`)
| ID | Estado | Descripción |
| :---: | :--- | :--- |
| `1` | Initialized | Carga inicializada en base de datos. |
| `2` | Pending | En espera en el sistema. |
| `3` | Ready | En cola de dosificación lista para ser batcheada. |
| `4` | Started | Dosificación iniciada por el operador. |
| `5` | Complete | Dosificación completada. |
| `6` | Canceled | Carga cancelada o abortada. |

---

## 4. Contratos por Recurso

---

### 4.1. Estado y Ping (`status/`)

#### Métodos y URLs
* `GET /rhino_rest_api/v1/json/status/`
* `GET /rhino_rest_api/v1/json/status/<result_id>`

#### Contrato de Respuesta: `StatusResponse`
```json
{
  "request_status": { "status": 200, "msg": "OK", "detail": "Ok" },
  "status": {
    "plant_code": "101",
    "plant_name": "North Yard",
    "plant_time": "2026-10-19 12:35:51",
    "api_version": "v1",
    "software_version": "1.4.10.6",
    "newest_result_id": 3,
    "results_pending": 1
  }
}
```

| Campo | Tipo | Descripción |
| :--- | :--- | :--- |
| `plant_code` | `string` | Código de la planta local. |
| `plant_name` | `string` | Nombre descriptivo de la planta. |
| `plant_time` | `string` | Fecha y hora local de la computadora (`YYYY-MM-DD HH:MM:SS`). |
| `api_version` | `string` | Versión del API (ej. `"v1"`). |
| `software_version` | `string` | Versión del software Concrete Rhino (ej. `"1.4.10.6"`). |
| `newest_result_id` | `long` | Último ID secuencial de resultado dosificado (`0` si no hay). |
| `results_pending` | `int` | Presente solo si se invoca con `<result_id>`. Cantidad de resultados más recientes que dicho ID. |

---

### 4.2. Plantas (`plants/`)

#### Métodos y URLs
* `GET /rhino_rest_api/v1/json/plants/`
* `GET /rhino_rest_api/v1/json/plants/<code>`
* `PUT /rhino_rest_api/v1/json/plants/` o `PUT /rhino_rest_api/v1/json/plants/<code>`
* `POST /rhino_rest_api/v1/json/plants/`

#### Campos del Registro `plant`
| Campo | Tipo | Max Len | GET | PUT/POST | Notas |
| :--- | :--- | :---: | :---: | :---: | :--- |
| `plant_code` | `string` | 7 | Sí | Requerido | Código de planta. Insensible a mayúsculas. |
| `plant_name` | `string` | 47 | Sí | Opcional | Nombre de la planta. |
| `plant_address` | `string` | 95 | Sí | Opcional | Dirección de la planta. |

*Nota particular:* En `plants/`, campos omitidos en el `PUT` se blanquean (no conservan su valor anterior). Se procesa registro por registro sin transacción.

---

### 4.3. Clientes (`customers/`, `customerlist/`)

#### Métodos y URLs
* `GET /rhino_rest_api/v1/json/customers/`
* `GET /rhino_rest_api/v1/json/customers/<cust_code>`
* `GET /rhino_rest_api/v1/json/customerlist/`
* `PUT /rhino_rest_api/v1/json/customers/` o `PUT /rhino_rest_api/v1/json/customers/<cust_code>`
* `POST /rhino_rest_api/v1/json/customers/`

#### Campos del Registro `customer`
| Campo | Tipo | Max Len | GET | PUT/POST | Reglas y Notas |
| :--- | :--- | :---: | :---: | :---: | :--- |
| `cust_code` | `string` | 15 | Sí | Requerido | Clave única de cliente. Insensible a mayúsculas. |
| `cust_name` | `string` | 64 | Sí | Req. en nuevo | Nombre o razón social. Sensible a mayúsculas en unicidad (409 si colisiona). |
| `cust_first_name` | `string` | 64* | Sí | Opcional | Se concatena con `cust_last_name` en un único contacto de 64 chars. |
| `cust_last_name` | `string` | 64* | No | Opcional | Ver `cust_first_name`. |
| `cust_address` | `string` | 95 | Sí | Opcional | Dirección de facturación. |
| `cust_bill_to` | `string` | 95 | No | Opcional | Dirección de facturación (sobreescribe a `cust_address` si ambos se envían). |
| `cust_phone` | `string` | 31 | Sí | Opcional | Teléfono principal. |
| `cust_mobile` | `string` | 31 | Sí | Opcional | Teléfono celular. |
| `cust_fax` | `string` | 31 | Sí | Opcional | Fax. |
| `cust_email` | `string` | 63 | Sí | Opcional | Correo electrónico. |
| `cust_udf1` | `int` | - | Sí | Opcional | Tipo de crédito: `1` Crédito, `2` COD (Contado), `3` Mal crédito. |
| `cust_udf2` | `int/float` | - | Sí (Desc) | **NO ENVIAR** | Gotcha: GET devuelve descuento; PUT asigna nivel de precio (1–3). Evitar en PUT. |
| `cust_plantcode` | `string` | 7 | No | Opcional | Si se envía, debe coincidir con la planta local (403 si difiere). |
| `cust_id` | `long` | - | No | Opcional | ID interno de registro si se desea asignar en creación. |

#### Contrato de Petición PUT / POST (`customers`)
```json
{
  "customers": [
    {
      "customer": {
        "cust_code": "JF100",
        "cust_name": "Johnson Flatwork",
        "cust_first_name": "Mike",
        "cust_last_name": "Johnson",
        "cust_address": "3211 Providence Dr, Anchorage, AK 99508",
        "cust_phone": "907-555-0100",
        "cust_udf1": 1
      }
    }
  ]
}
```

#### Contrato de Respuesta: `customerlist`
```json
{
  "request_status": { "status": 200, "msg": "OK", "detail": "Ok" },
  "customerlist": [
    {
      "listitem": {
        "cust_code": "JF100",
        "cust_name": "Johnson Flatwork"
      }
    }
  ]
}
```

---

### 4.4. Materiales (`materials/`, `materiallist/`)

#### Métodos y URLs
* `GET /rhino_rest_api/v1/json/materials/`
* `GET /rhino_rest_api/v1/json/materials/<mat_code>`
* `GET /rhino_rest_api/v1/json/materiallist/`
* `PUT /rhino_rest_api/v1/json/materials/` o `PUT /rhino_rest_api/v1/json/materials/<mat_code>`
* *(POST y DELETE no están permitidos; devuelven 409 y 405).*

#### Campos del Registro `material`
| Campo | Tipo | Max Len | GET | PUT | Reglas y Notas |
| :--- | :--- | :---: | :---: | :---: | :--- |
| `mat_code` | `string` | 19 | Sí | Requerido | Código de material. **Sensible a mayúsculas**. |
| `mat_name` | `string` | 19 | Sí | Ignorado | Espejo de `mat_code`. |
| `mat_plantcode` | `string` | 7 | Sí | Opcional | Si se envía, debe coincidir con la planta local (403). |
| `mat_type` | `int` | - | Sí | Opcional | Código de tipo (1–8). Debe pertenecer a la misma familia del material almacenado (409). |
| `mat_source` | `string` | 9 | Sí | Opcional | Origen o banco de material (ej. `"Pit 22"`). |
| `mat_spgr` | `float` | - | Sí | Opcional | Gravedad específica ($0 < spgr \le 10$). |
| `mat_absorption`| `float` | - | Sí | Agregados | % Absorción (solo agregados). |
| `mat_totalmoisture`| `float`| - | Sí | Agregados | % Humedad total (solo agregados). |
| `mat_dosagetype`| `string/int`| - | Sí | Aditivos | `0` por yd³/m³; `1` o `2` por quintal de cemento (cwt). En GET puede retornar `"0"` o `"1"`. |
| `mat_ssd` | `string/int`| - | Sí | Ignorado | Siempre retornado como `"1"` para agregados. |
| `mat_order` | `int` | - | Sí | Ignorado | Siempre `0`. |
| `mat_price` | `float` | - | Sí | Ignorado | Precio de costo del material. |

#### Contrato de Petición PUT (`materials`)
```json
{
  "materials": [
    {
      "material": {
        "mat_plantcode": "101",
        "mat_code": "Sand",
        "mat_type": 3,
        "mat_source": "Pit 22",
        "mat_spgr": 2.61,
        "mat_absorption": 1.2,
        "mat_totalmoisture": 4.4
      }
    }
  ]
}
```

---

### 4.5. Diseños de Mezcla (`mixdesigns/`, `mixdesignlist/`)

#### Métodos y URLs
* `GET /rhino_rest_api/v1/json/mixdesigns/`
* `GET /rhino_rest_api/v1/json/mixdesigns/<mix_code>`
* `GET /rhino_rest_api/v1/json/mixdesignlist/`
* `PUT /rhino_rest_api/v1/json/mixdesigns/` o `PUT /rhino_rest_api/v1/json/mixdesigns/<mix_code>`
* `POST /rhino_rest_api/v1/json/mixdesigns/`

#### Campos del Bloque Cabecera `mixinfo`
| Campo | Tipo | Max Len | Defecto | PUT/POST | Notas |
| :--- | :--- | :---: | :---: | :---: | :--- |
| `mix_code` | `string` | 19 | - | Requerido | Código de receta. Insensible a mayúsculas. |
| `mix_name` | `string` | 63 | - | Req. nuevo | Descripción comercial de la receta. |
| `mix_unittype` | `int` | - | `0` | Opcional | `0`: US (`yd³`), `1`: Métrico (`m³`). Modificarlo exige enviar `mixcomponents`. |
| `mix_price` | `float` | - | `0` | Opcional | Precio unitario base ($\ge 0$). |
| `mix_slump` | `float` | - | `4` | Opcional | Revenimiento de diseño ($\ge 0$). |
| `mix_maxsize` | `float` | - | `10` | Opcional | Tamaño máximo de viaje/carga ($> 0$). |
| `mix_mixtime` | `float` | - | `0` | Opcional | Tiempo de mezclado en minutos ($\ge 0$). |
| `mix_air` | `float` | - | `0` | Opcional | Contenido de aire objetivo ($\ge 0$). |
| `mix_ssd` | `int` | - | `1` | Opcional | `0`: Pesos base seca (oven-dry), `1`: Base saturada seca (SSD). |
| `mix_yield` | `float` | - | `1` | Opcional | Rendimiento volumétrico ($\ge 0$). |
| `mix_notes` | `string` | 63 | `""` | Opcional | Observaciones de la mezcla. |
| `mix_plantcode`| `string` | 7 | - | Opcional | Si se envía, debe coincidir con la planta local (403). |

#### Campos del Bloque Detalle `mixcomponent` (hasta 24 componentes)
| Campo | Tipo | Max Len | En PUT/POST | Notas |
| :--- | :--- | :---: | :---: | :--- |
| `mc_code` | `string` | 19 | Requerido | Código del material existente en la planta. Sensible a mayúsculas. |
| `mc_quantity` | `float` | - | Requerido | Cantidad dosificada por yd³ o m³ ($\ge 0$). |
| `mc_unit` | `int` | - | Opcional | Unidad de medida. Debe coincidir con la configurada en la tolva de planta. |
| `mc_order` | `int` | - | Opcional | Secuencia de dosificación en báscula ($\ge 0$). |
| `mc_type` | `int` | - | Opcional | Familia del material (debe coincidir con la del material; 409 si difiere). |
| `mc_mixcode` | `string` | 19 | Opcional | Código de la receta padre. |
| `mc_name`, `mc_source`, `mc_spgr`, `mc_absorption` | - | - | Ignorados | Se configuran mediante `materials/`. |

#### Contrato de Petición PUT / POST (`mixdesigns`)
```json
{
  "mixdesigns": [
    {
      "mixdesign": {
        "mixinfo": {
          "mix_code": "4000EXT",
          "mix_name": "4000 PSI Exterior",
          "mix_price": 131.50,
          "mix_slump": 5.0,
          "mix_mixtime": 1.5,
          "mix_air": 6.0,
          "mix_notes": "air entrained"
        },
        "mixcomponents": [
          { "mixcomponent": { "mc_code": "Sand", "mc_quantity": 1800.0 } },
          { "mixcomponent": { "mc_code": "Cement", "mc_quantity": 564.0 } },
          { "mixcomponent": { "mc_code": "Water", "mc_quantity": 32.0 } },
          { "mixcomponent": { "mc_code": "AEA", "mc_quantity": 22.5 } }
        ]
      }
    }
  ]
}
```

---

### 4.6. Órdenes (`orders/`, `orderslist/`, `orderstatus/`, `orderloads/`)

#### Métodos y URLs
* `GET /rhino_rest_api/v1/json/orders/` (órdenes abiertas, estados 1–5)
* `GET /rhino_rest_api/v1/json/orders/<date>` o `orders/<from>/<to>` (máx. 93 días)
* `GET /rhino_rest_api/v1/json/orders/<order_num>`
* `GET /rhino_rest_api/v1/json/orderslist/...`
* `GET /rhino_rest_api/v1/json/orderstatus/<order_num>`
* `PUT /rhino_rest_api/v1/json/orderstatus/` o `orderstatus/<order_num>`
* `GET /rhino_rest_api/v1/json/orderloads/<order_num>`
* `PUT /rhino_rest_api/v1/json/orders/` o `PUT /rhino_rest_api/v1/json/orders/<order_num>`
* `POST /rhino_rest_api/v1/json/orders/`

#### Campos de Escritura en Registro `order` (PUT / POST)
| Campo | Tipo | Max Len | En Creación | En Modificación | Reglas |
| :--- | :--- | :---: | :---: | :---: | :--- |
| `order_num` | `string` | 31 | Requerido | Opcional | Número de orden del despacho. |
| `cust_code` | `string` | 15 | Requerido | Opcional | Código de cliente válido en la planta. |
| `mix_code` | `string` | 19 | Requerido | Condicional | Código de mezcla existente. Solo modificable si no hay cargas y estado es 1–3. |
| `quantity` | `float` | - | Requerido | Opcional | Volumen total ordenado ($> 0$). |
| `status_id` | `int` | - | Opcional (def 1) | Opcional | Estado 1 a 7 (no puede crearse directamente en 6 o 7). |
| `wanted_date`| `string`| - | Opcional | Opcional | Formato `"YYYY-MM-DD"` o `"YYYY-MM-DD HH:MM"`. |
| `wanted_time`| `string`| - | Opcional | Opcional | Formato `"HH:MM"`. Si no se envía toma `"06:00"`. |
| `load_size` | `float` | - | Opcional | Opcional | Tamaño sugerido de viaje ($> 0$). Recomendado enviar. |
| `num_loads` | `int` | - | Opcional | Opcional | No se almacena; calcula cantidad de productos extra por carga. |
| `delivery_addrs`| `string`| 95 | Opcional | Opcional | Dirección de entrega en obra. |
| `po_num` | `string` | 31 | Opcional | Opcional | Orden de compra del cliente. |
| `slump` | `float` | - | Opcional (def 3)| Opcional | Revenimiento solicitado. |
| `notes` | `string` | 255 | Opcional | Opcional | Instrucciones especiales para la entrega. |
| `mix_price` | `float` | - | Opcional | Opcional | Precio unitario pactado ($\ge 0$). Si se omite, toma el catálogo. |
| `plant_code` | `string` | 7 | Opcional | Opcional | Debe coincidir con la planta local (403). |

#### Campos Retornados en Lectura `order` (GET)
Además de los campos anteriores, GET retorna campos calculados e internos:
* `qty_required` (`float`): Volumen total solicitado.
* `qty_remaining` (`float`): Volumen pendiente por dosificar.
* `customer` (`string`): Nombre del cliente (¡no su `cust_code`!).
* `contact`, `phone_number`, `cell_number`, `discount`: Datos tomados del cliente.
* `description` (`string`): Nombre descriptivo de la receta.
* `price` (`float`): Precio por unidad de la mezcla aplicado a la orden.
* `mix_time` (`int`): Tiempo de mezclado en **segundos** (a diferencia de `mix_mixtime` en recetas que es en minutos).
* `travel_time` (`int`): Tiempo de viaje estimado en minutos (defecto 15).
* `spacing` (`int`): Espaciado entre camiones en minutos (defecto 30).
* `last_load_start` (`string`): Timestamp del inicio del último viaje dosificado (`"YYYY-MM-DD HH:MM:SS"` o `""`).

#### Contrato de Cambio de Estado: `orderstatus` (PUT)
```json
{
  "orderstatus": [
    {
      "order": {
        "order_num": "445676",
        "status_id": 6
      }
    }
  ]
}
```

---

### 4.7. Tickets de Carga (`tickets/`)

#### Métodos y URLs
* `PUT /rhino_rest_api/v1/json/tickets/` o `PUT /rhino_rest_api/v1/json/tickets/<ticket_num>`
* `POST /rhino_rest_api/v1/json/tickets/`
* `DELETE /rhino_rest_api/v1/json/tickets/<ticket_num>`

#### Campos del Registro `ticket` (PUT / POST)
| Campo | Tipo | Max Len | En Creación | En Modificación | Reglas |
| :--- | :--- | :---: | :---: | :---: | :--- |
| `ticket_num` | `long` | - | Opcional | Identificador | Número entero $> 0$. Si se omite, la planta lo numera al imprimir y no podrá modificarse vía API. |
| `order_num` | `string` | 31 | Requerido | Requerido | Número de orden asociada. |
| `load_size` | `float` | - | Requerido | Opcional | Tamaño del viaje ($> 0$). |
| `truck_num` | `string` | 15 | Requerido | Opcional | Número de camión. Si no existe en planta, se crea automáticamente. |
| `driver_name`| `string` | 15 | Opcional | Opcional | Nombre del chofer. Se auto-crea si no existe en planta. |
| `message` | `string` | 255 | Opcional | Opcional | Mensaje impreso en la boleta/ticket. |
| `order` | `object` | - | Condicional | Opcional | Bloque idéntico a PUT `orders`. Mandatorio si la orden aún no existe en la planta. |
| `extras` | `array` | 13 items | Opcional | Reemplazo | Arreglo de productos adicionales asociados al ticket. |
| `totals` | `object` | - | Opcional | Reemplazo | Totales fiscales y comerciales para impresión. |

#### Sub-bloque `extras` (Productos Adicionales en Ticket)
| Campo | Tipo | Max Len | Obligatorio | Notas |
| :--- | :--- | :---: | :---: | :--- |
| `code` | `string` | 19 | Sí | Código de producto en planta que no sea una mezcla (ej. `"FUEL"`, `"FIBER"`). |
| `load_qty` | `float` | - | Sí | Cantidad asignada a este viaje en particular ($\ge 0$). |
| `order_qty` | `float` | - | Opcional | Acumulado total de la orden. Actualiza la línea de facturación de la orden. |
| `price` | `float` | - | Opcional | Precio unitario. Si se omite, usa el precio de catálogo de planta. |
| `amount` | `float` | - | Opcional | Total de línea en ticket ($\ge 0$). Si se omite, calcula `load_qty * price`. |
| `description`| `string`| 15 | Opcional | Descripción impresa en ticket. |
| `unit` | `string` | 3 | Opcional | Símbolo de unidad impreso en ticket (ej. `"EA"`, `"LBS"`). |

#### Sub-bloque `totals` (Totales de Impresión de Boleta)
| Campo | Tipo | Descripción |
| :--- | :--- | :--- |
| `subtotal` | `float` | Subtotal antes de impuestos ($\ge 0$). |
| `tax` | `float` | Importe de impuestos ($\ge 0$). |
| `cumulative_total` | `float` | Total acumulado facturado de la orden ($\ge 0$). |
| `grand_total` | `float` | Total general a cobrar en este ticket ($\ge 0$). |

#### Contrato de Petición Completa PUT (`tickets`)
```json
{
  "tickets": [
    {
      "ticket": {
        "order_num": "445676",
        "load_size": 7.0,
        "truck_num": "12",
        "driver_name": "Steve",
        "message": "Entrar por porton sur",
        "order": {
          "cust_code": "JOHN01",
          "mix_code": "3000INT",
          "quantity": 14.0,
          "mix_price": 118.50,
          "slump": 5.0,
          "wanted_date": "2026-10-20",
          "wanted_time": "13:30"
        },
        "extras": [
          {
            "code": "FUEL",
            "load_qty": 1.0,
            "order_qty": 1.0,
            "price": 15.00,
            "description": "Fuel Surcharge",
            "unit": "EA"
          }
        ],
        "totals": {
          "subtotal": 844.50,
          "tax": 46.45,
          "grand_total": 890.95
        }
      }
    }
  ]
}
```

#### Contrato de Respuesta: Creación/Modificación de Ticket
```json
{
  "request_status": { "status": 201, "msg": "CREATED", "detail": "Created" },
  "tickets": [
    {
      "ticket": {
        "ticket_num": 100050,
        "load_id": 1,
        "order_num": "445676",
        "result": "created",
        "added_at_plant": ["order 445676", "driver Steve", "truck 12"]
      }
    }
  ]
}
```

#### Contrato de Respuesta: Eliminación de Ticket (`DELETE`)
```json
{
  "request_status": { "status": 200, "msg": "OK", "detail": "Ok" },
  "ticket": {
    "ticket_num": 100050,
    "order_num": "445676",
    "result": "deleted"
  }
}
```

---

### 4.8. Cargas de Despacho (`loads/`, `loadslist/`)

#### Métodos y URLs
* `GET /rhino_rest_api/v1/json/loads/` (todas las cargas no canceladas)
* `GET /rhino_rest_api/v1/json/loads/<date>` o `loads/<from>/<to>`
* `GET /rhino_rest_api/v1/json/loads/<ticket>`
* `GET /rhino_rest_api/v1/json/loadslist/...`

#### Contrato de Respuesta: `loads`
```json
{
  "request_status": { "status": 200, "msg": "OK", "detail": "Ok" },
  "loads": [
    {
      "loads": {
        "load": {
          "plantcode": "101",
          "order_num": "445676",
          "ticket": 100050,
          "status": 5,
          "load_date": "2026-10-20",
          "load_time": "14:05:12",
          "load_size": 7.0,
          "mix_code": "3000INT",
          "mix_id": 12,
          "mix_descr": "3000 PSI Interior",
          "truck_id": "12",
          "driver_id": "3",
          "driver_name": "Steve",
          "driver_phone": "",
          "deliv_stat_id": 1,
          "deliv_stat": "En route",
          "batches": 2,
          "hundred_wts": 39.48,
          "water_in_truck": 0.0,
          "water_adjust": 0.0,
          "water_trim": 0.0,
          "message": "Entrar por porton sur",
          "last": 0,
          "returned_qty": 0.0,
          "customer_name": "Johnson Flatwork",
          "batch_man": "Operator 1"
        },
        "batchcomponents": [
          {
            "batchcomponent": {
              "bc_ticket": 100050,
              "bc_matcode": "Sand",
              "bc_mattype": 3,
              "bc_design": 1800.0,
              "bc_target": 12600.0,
              "bc_actual": 12640.0,
              "bc_unit": 6,
              "bc_freemoisture": 3.2
            }
          }
        ]
      }
    }
  ]
}
```

---

### 4.9. Resultados de Dosificación (`batchresults/`, `batchresultlist/`)

#### Métodos y URLs
* `GET /rhino_rest_api/v1/json/batchresults/since/<result_id>` (máx. 100 por página)
* `GET /rhino_rest_api/v1/json/batchresults/<date>` o `batchresults/<from>/<to>`
* `GET /rhino_rest_api/v1/json/batchresults/<mix_code>/<from>/<to>`
* `GET /rhino_rest_api/v1/json/batchresults/<ticket>`
* `GET /rhino_rest_api/v1/json/batchresults/` (todas las cargas de la planta)
* `GET /rhino_rest_api/v1/json/batchresultlist/...`

#### Contrato de Respuesta: `batchresults`
```json
{
  "request_status": { "status": 200, "msg": "OK", "detail": "Ok" },
  "more": 0,
  "batchresults": [
    {
      "batchresult": {
        "batchinfo": {
          "bt_ticket": 100050,
          "bt_plantcode": "101",
          "bt_mixcode": "3000INT",
          "bt_custcode": 45,
          "bt_jobcode": "445676",
          "bt_loaddate": "2026-10-20",
          "bt_loadtime": "14:05:12",
          "bt_loadsize": 7.0,
          "bt_waterintruck": 0.0,
          "bt_watertrim": 0.0,
          "bt_batcher": "Operator 1",
          "bt_truck": "12",
          "bt_driver": "Steve",
          "bt_resultid": 3
        },
        "batchcomponents": [
          {
            "batchcomponent": {
              "bc_ticket": 100050,
              "bc_matcode": "Sand",
              "bc_mattype": 3,
              "bc_design": 1800.0,
              "bc_target": 12600.0,
              "bc_actual": 12640.0,
              "bc_unit": 6,
              "bc_freemoisture": 3.2
            }
          },
          {
            "batchcomponent": {
              "bc_ticket": 100050,
              "bc_matcode": "Cement",
              "bc_mattype": 6,
              "bc_design": 564.0,
              "bc_target": 3948.0,
              "bc_actual": 3955.0,
              "bc_unit": 6,
              "bc_freemoisture": 0.0
            }
          }
        ]
      }
    }
  ]
}
```

#### Descripción de Campos de Resultados
* **`more` (`int`):** Solo en peticiones `/since/<result_id>`. Vale `1` si el lote alcanzó el límite de 100 registros (requiere solicitar la siguiente página a partir del último `bt_resultid` recibido); `0` si no hay más registros pendientes.
* **`batchinfo`:**
  * `bt_ticket` (`long`): Número de boleta / ticket dosificado.
  * `bt_plantcode` (`string`): Código de la planta.
  * `bt_mixcode` (`string`): Código de la receta.
  * `bt_custcode` (`long`): ID numérico interno del cliente (`0` si el cliente fue borrado).
  * `bt_jobcode` (`string`): Número de orden de despacho.
  * `bt_loaddate` / `bt_loadtime`: Fecha y hora de finalización de dosificación.
  * `bt_loadsize` (`float`): Tamaño del viaje dosificado.
  * `bt_waterintruck` / `bt_watertrim` (`float`): Ajustes de agua agregada/recortada.
  * `bt_batcher` / `bt_truck` / `bt_driver` (`string`): Operador, camión y chofer.
  * `bt_resultid` (`long`): ID incremental secuencial (`0` si la carga no ha finalizado).
* **`batchcomponent`:**
  * `bc_ticket` (`long`): Número de ticket.
  * `bc_matcode` (`string`): Código del material.
  * `bc_mattype` (`int`): Familia de material (1–8).
  * `bc_design` (`float`): Cantidad teórica según diseño de mezcla por unidad (yd³ o m³).
  * `bc_target` (`float`): Peso objetivo calculado para el volumen del viaje.
  * `bc_actual` (`float`): Peso real efectivamente pesado en báscula.
  * `bc_unit` (`int`): Código de unidad de medida.
  * `bc_freemoisture` (`float`): % de humedad libre compensada en la báscula.

---

## 5. Matriz de Códigos de Respuesta y Errores

| `status` | `msg` | `detail` | Causa / Disparador |
| :---: | :--- | :--- | :--- |
| **200** | `OK` | `Ok` | Consulta GET exitosa con datos, o DELETE exitoso de ticket. |
| **201** | `CREATED` | `Created` | PUT o POST creó al menos un nuevo registro. |
| **201** | `UPDATED` | `Updated` | PUT modificó registros ya existentes (sin crear nuevos). |
| **204** | `NO CONTENT` | `Database query did not return any results.` | Consulta GET no arrojó registros (retorna array vacío). |
| **400** | `BAD REQUEST` | `JSON Error` | Error de parseo JSON, campos requeridos faltantes, valores fuera de rango o negativos, códigos excediendo longitud máxima, o cuerpo sin arreglo contenedor. |
| **400** | `BAD REQUEST` | `API URL inconsistant with body` | El código de la URL difiere del código en el cuerpo, falta código obligatorio, o la URL no cumple formato de fecha/ticket. |
| **400** | `BAD REQUEST` | `Unknown cust_code.` | La orden especifica un cliente inexistente o eliminado en planta. |
| **400** | `BAD REQUEST` | `Unknown mix_code.` | La orden especifica una receta inexistente o eliminada en planta. |
| **400** | `BAD REQUEST` | `Unsupported format.` | Se especificó un formato distinto a JSON en la URL. |
| **400** | `BAD REQUEST` | *(vacío)* | Cuerpo vacío o sintácticamente no es un JSON válido. |
| **401** | `UNAUTHORIZED` | `No log in.` | Token ausente, token inválido/inexistente, o IP temporalmente bloqueada por 10 fallos. |
| **403** | `FORBIDDEN` | `Forbidden` | Token de solo lectura (Read-Only) intentando ejecutar PUT, POST o DELETE. |
| **403** | `FORBIDDEN` | `plant_code is not this plant.` | El `plant_code` especificado en el registro difiere del código de esta planta. |
| **404** | `NOT FOUND` | `Record not found.` | Recurso no existe (material, orden en cambio de estado, ticket, o producto extra en ticket). |
| **404** | `NOT FOUND` | `Unsupported resource.` | Intento de PUT/POST a un código de planta inexistente. |
| **404** | `NOT FOUND` | *(vacío)* | Nombre de recurso de la API no reconocido en la URL. |
| **405** | `METHOD NOT ALLOWED` | *(vacío)* | DELETE invocado sobre cualquier recurso que no sea `tickets/<ticket_num>`. |
| **409** | `CONFLICT` | `Http POST - Record already exists.` | POST sobre un registro que ya existe, POST a `orderstatus`, o renombrado sobre código en uso. |
| **409** | `CONFLICT` | `Order already has loads.` | Intento de cambiar la receta (`mix_code`) de una orden que ya tiene viajes dosificados o en proceso. |
| **409** | `CONFLICT` | `Conflicts with what is stored.` | Modificar o borrar un ticket que ya empezó a dosificar o fue cancelado; camión borrado en planta; receta en dosificación activa; cambio de familia de material. |
| **409** | `CONFLICT` | `Database Error.` | La base de datos de la planta rechazó la inserción/actualización. |
| **501** | `NOT IMPLEMENTED` | *(vacío)* | Método HTTP no implementado (ej. PATCH, HEAD). |
| **503** | `SERVICE UNAVAILABLE` | `Database is busy.` | Base de datos de la planta ocupada; reintentar en unos segundos. |
