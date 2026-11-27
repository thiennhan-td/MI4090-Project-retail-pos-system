const supplierAmount = document.querySelector('#pay-amount-input');
const supplierStatus = document.querySelector('#supplier-pay-status');
const supplierConfirm = document.querySelector('#supplier-pay-confirm');
const supplierRest = document.querySelector('#supplier-pay-rest');
const supplierOne = document.querySelector('#supplier-pay-one');
let supplierBalance = Number(supplierAmount.max);
for (const control of [supplierRest, supplierOne]) control.removeAttribute('onclick');
supplierRest.addEventListener('click', () => { supplierAmount.value = String(supplierBalance); });
supplierOne.addEventListener('click', () => { supplierAmount.value = String(Math.min(1000000, supplierBalance)); });
supplierConfirm.addEventListener('click', () => {
  const amount = supplierAmount.valueAsNumber;
  if (!Number.isSafeInteger(amount) || amount <= 0 || amount > supplierBalance) {
    supplierStatus.textContent = 'Số tiền cần lớn hơn 0 và không vượt dư nợ còn lại (' + formatDemoMoney(supplierBalance) + ').';
    return;
  }
  supplierBalance -= amount;
  supplierAmount.value = '';
  supplierAmount.max = String(supplierBalance);
  supplierConfirm.disabled = supplierBalance === 0;
  supplierRest.disabled = supplierOne.disabled = supplierBalance === 0;
  supplierRest.textContent = 'Trả hết nợ (' + formatDemoMoney(supplierBalance) + ')';
  supplierOne.textContent = 'Trả ' + formatDemoMoney(Math.min(1000000, supplierBalance));
  for (const balance of document.querySelectorAll('[data-supplier-balance]')) {
    balance.textContent = formatDemoMoney(supplierBalance);
  }
  supplierStatus.textContent = 'Đã ghi khoản chi mẫu ' + formatDemoMoney(amount) +
    '. Dư nợ còn lại: ' + formatDemoMoney(supplierBalance) + '.';
});
