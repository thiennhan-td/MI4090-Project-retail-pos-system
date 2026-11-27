const products = [...document.querySelectorAll('.product-card')];
const categoryButtons = [...document.querySelectorAll('.cat-btn')];
const cart = new Map();
const payments = [];
let activeCategory = 'all';
let confirmed = false;

const saleStatus = document.querySelector('#sale-status');
const cartView = document.querySelector('#cart-items');
const paymentForm = document.querySelector('#sale-payment-form');
const saleAmount = document.querySelector('#sale-amount');
const totalDue = () => [...cart.values()].reduce((sum, line) => sum + line.qty * Number(line.product.dataset.price), 0);
const totalPaid = () => payments.reduce((sum, payment) => sum + payment.amount, 0);

function saleMessage(message, error = false) {
  saleStatus.textContent = message;
  saleStatus.classList.toggle('demo-error', error);
}

function filterProducts() {
  const query = document.querySelector('#sale-search').value.trim().toLocaleLowerCase('vi');
  for (const product of products) {
    const categoryMatches = activeCategory === 'all' || product.dataset.cat === activeCategory;
    const queryMatches = (product.dataset.id + ' ' + product.dataset.name).toLocaleLowerCase('vi').includes(query);
    product.hidden = !categoryMatches || !queryMatches;
    product.disabled = confirmed;
  }
  document.querySelector('#sale-empty').hidden = products.some(product => !product.hidden);
  for (const button of categoryButtons) {
    button.setAttribute('aria-pressed', String(button.dataset.cat === activeCategory));
  }
}

function makeCartButton(label, accessibleName, onClick) {
  const button = document.createElement('button');
  button.type = 'button';
  button.className = 'demo-action';
  button.textContent = label;
  button.setAttribute('aria-label', accessibleName);
  button.disabled = confirmed;
  button.addEventListener('click', onClick);
  return button;
}

function setQuantity(product, qty) {
  if (qty > Number(product.dataset.stock)) {
    saleMessage('Số lượng vượt tồn mẫu của ' + product.dataset.name + '.', true);
    return;
  }
  if (qty <= 0) cart.delete(product.dataset.id);
  else cart.set(product.dataset.id, { product, qty });
  saleMessage('');
  renderSale();
}

function renderSale() {
  cartView.replaceChildren();
  if (!cart.size) cartView.textContent = 'Chưa có mặt hàng trong hóa đơn.';
  for (const { product, qty } of cart.values()) {
    const { name, price } = product.dataset;
    const row = document.createElement('div');
    row.className = 'demo-panel demo-stack';
    const item = document.createElement('strong');
    item.textContent = name;
    const amount = document.createElement('span');
    amount.textContent = qty + ' × ' + formatDemoMoney(Number(price)) + ' = ' + formatDemoMoney(qty * Number(price));
    const controls = document.createElement('div');
    controls.className = 'demo-controls';
    controls.append(
      makeCartButton('−', 'Giảm số lượng ' + name, () => setQuantity(product, qty - 1)),
      makeCartButton('+', 'Tăng số lượng ' + name, () => setQuantity(product, qty + 1)),
      makeCartButton('Xóa', 'Xóa ' + name, () => setQuantity(product, 0))
    );
    row.append(item, amount, controls);
    cartView.append(row);
  }
  const list = document.querySelector('#payment-list');
  list.replaceChildren(...payments.map((payment, index) => {
    const row = document.createElement('li');
    row.className = 'demo-summary';
    row.append((payment.method === 'cash' ? 'Tiền mặt' : 'Chuyển khoản') + ' · ' + formatDemoMoney(payment.amount),
      makeCartButton('Xóa', 'Xóa khoản thanh toán ' + (index + 1), () => {
        payments.splice(index, 1);
        saleMessage('');
        renderSale();
      }));
    return row;
  }));
  const received = totalPaid();
  const due = totalDue();
  document.querySelector('#grand-total').textContent = formatDemoMoney(due);
  document.querySelector('#paid-total').textContent = formatDemoMoney(received);
  document.querySelector('#due-total').textContent = formatDemoMoney(Math.max(0, due - received));
  document.querySelector('#change-total').textContent = formatDemoMoney(Math.max(0, received - due));
  document.querySelector('#sale-confirm').disabled = confirmed;
  document.querySelector('#sale-print').hidden = !confirmed;
  const newSale = document.querySelector('#sale-new');
  newSale.textContent = confirmed ? 'Hóa đơn mới' : 'Hủy hóa đơn nháp';
  newSale.disabled = !confirmed && cart.size === 0;
  document.querySelector('#sale-state').textContent = confirmed ? 'Đã xác nhận · Dữ liệu mẫu' : 'Nháp · Ca đang mở';
  for (const control of paymentForm.elements) control.disabled = confirmed;
  filterProducts();
}

for (const product of products) {
  product.addEventListener('click', () => setQuantity(product, (cart.get(product.dataset.id)?.qty ?? 0) + 1));
  const image = product.querySelector('img');
  if (!image) continue;
  const showFallback = () => {
    if (!image.isConnected) return;
    const fallback = document.createElement('span');
    fallback.className = 'demo-product-mark';
    fallback.setAttribute('aria-hidden', 'true');
    fallback.textContent = product.dataset.name.split(/\s+/).slice(0, 2).map(word => word[0]).join('').toLocaleUpperCase('vi');
    image.replaceWith(fallback);
  };
  image.addEventListener('error', showFallback, { once: true });
  if (image.complete && !image.naturalWidth) showFallback();
}
for (const button of categoryButtons) button.addEventListener('click', () => {
  activeCategory = button.dataset.cat;
  filterProducts();
});
document.querySelector('#sale-search').addEventListener('input', filterProducts);
paymentForm.addEventListener('submit', event => {
  event.preventDefault();
  const method = document.querySelector('#sale-method').value;
  const amount = saleAmount.valueAsNumber;
  if (!cart.size || !Number.isSafeInteger(amount) || amount <= 0) return saleMessage('Thêm mặt hàng và nhập số tiền hợp lệ.', true);
  const remaining = totalDue() - totalPaid();
  if (remaining <= 0 || (method === 'transfer' && amount > remaining)) {
    return saleMessage('Kiểm tra số còn phải thu. Chuyển khoản không được vượt số này.', true);
  }
  payments.push({ method, amount });
  saleAmount.value = '';
  saleMessage('');
  renderSale();
});
document.querySelector('#sale-confirm').addEventListener('click', () => {
  const due = totalDue();
  const transfer = payments.filter(payment => payment.method === 'transfer').reduce((total, payment) => total + payment.amount, 0);
  if (!cart.size || totalPaid() < due || transfer > due) {
    return saleMessage('Hóa đơn cần có mặt hàng và được thanh toán đủ bằng hai phương thức hợp lệ.', true);
  }
  confirmed = true;
  saleMessage('Đã xác nhận hóa đơn mẫu. Có thể in hóa đơn.');
  renderSale();
});
document.querySelector('#sale-print').addEventListener('click', () => window.print());
document.querySelector('#sale-new').addEventListener('click', () => {
  if (!confirmed && (cart.size || payments.length) && !window.confirm('Bỏ hóa đơn nháp hiện tại?')) return;
  const discarded = !confirmed;
  confirmed = false;
  cart.clear();
  payments.length = 0;
  paymentForm.reset();
  saleMessage(discarded ? 'Đã hủy hóa đơn nháp.' : '');
  renderSale();
});
renderSale();
