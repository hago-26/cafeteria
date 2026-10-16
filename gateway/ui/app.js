// Rutas relativas: el gateway decide a qué servicio va cada /api/*
const API = { catalogo: "/api/catalogo", pedidos: "/api/pedidos" };

const $ = (sel) => document.querySelector(sel);
const dinero = (n) => Number(n).toLocaleString("es-MX", { style: "currency", currency: "MXN" });

async function llamar(url, opciones) {
  const resp = await fetch(url, opciones);
  $("#servido-por").textContent = resp.headers.get("X-Servido-Por") ?? "(sin cabecera)";
  const cuerpo = resp.headers.get("content-type")?.includes("json") ? await resp.json() : null;
  return { ok: resp.ok, status: resp.status, cuerpo };
}

async function cargarMenu() {
  const tbody = $("#menu tbody");
  try {
    const { ok, cuerpo } = await llamar(`${API.catalogo}/productos`);
    if (!ok) throw new Error();
    tbody.innerHTML = cuerpo.map((p) => `
      <tr class="${p.disponible ? "" : "agotado"}">
        <td>${p.nombre}${p.disponible ? "" : " <small>(agotado)</small>"}</td>
        <td>${dinero(p.precio)}</td>
        <td><input type="number" min="0" value="0" data-id="${p.id}"></td>
      </tr>`).join("");
  } catch {
    tbody.innerHTML = `<tr><td colspan="3">No se pudo cargar el catálogo.</td></tr>`;
  }
}

async function crearPedido() {
  const items = [...document.querySelectorAll("#menu input")]
    .map((i) => ({ productoId: Number(i.dataset.id), cantidad: Number(i.value) }))
    .filter((i) => i.cantidad > 0);

  const msg = $("#resultado");
  const { ok, status, cuerpo } = await llamar(`${API.pedidos}/pedidos`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ items }),
  });

  if (ok) {
    msg.className = "msg ok";
    msg.textContent = `Pedido #${cuerpo.id} creado. Total: ${dinero(cuerpo.total)}`;
    cargarPedidos();
  } else {
    msg.className = "msg error";
    const errores = cuerpo?.errors ? Object.values(cuerpo.errors).flat().join(" ") : "";
    msg.textContent = `Error ${status}: ${cuerpo?.title ?? ""} ${errores}`;
  }
}

async function cargarPedidos() {
  const ul = $("#pedidos");
  const { ok, cuerpo } = await llamar(`${API.pedidos}/pedidos`);
  ul.innerHTML = ok
    ? cuerpo.map((p) => `<li>#${p.id} · ${p.items.map((i) => `${i.cantidad}× ${i.nombre}`).join(", ")} · <b>${dinero(p.total)}</b></li>`).join("") || "<li>Sin pedidos.</li>"
    : "<li>No se pudo cargar.</li>";
}

async function probarBalanceo(servicio) {
  const conteo = {};
  for (let i = 0; i < 10; i++) {
    const { ok, cuerpo } = await llamar(`${API[servicio]}/info`);
    const host = ok ? cuerpo.host : "error";
    conteo[host] = (conteo[host] ?? 0) + 1;
  }
  $("#hosts").innerHTML = Object.entries(conteo)
    .map(([host, n]) => `<li><code>${host}</code> → ${n} respuesta(s)</li>`).join("");
}

$("#btn-pedir").addEventListener("click", crearPedido);
$("#btn-pedidos").addEventListener("click", cargarPedidos);
document.querySelectorAll(".btn-balanceo").forEach((b) =>
  b.addEventListener("click", () => probarBalanceo(b.dataset.servicio)));

cargarMenu();
cargarPedidos();
