const accountBody = document.querySelector('#tableBody');
const accountCreate = document.querySelector('#createAccountForm');
const accountRoles = {
  QUAN_LY: 'Quản lý',
  THU_NGAN: 'Thu ngân',
  NHAN_VIEN_KHO: 'Nhân viên kho',
};
const accountStatus = document.createElement('p');
accountStatus.className = 'demo-status';
accountStatus.setAttribute('role', 'status');
document.querySelector('#accountsTable').parentElement.after(accountStatus);

function updateAccountCounts() {
  const rows = [...accountBody.rows];
  for (const role of ['ALL', ...Object.keys(accountRoles)]) {
    const button = document.querySelector('#filter-btn-' + role);
    const count = role === 'ALL' ? rows.length : rows.filter(row => row.dataset.role === role).length;
    button.textContent = (accountRoles[role] ?? 'Tất cả') + ' (' + count + ')';
  }
}

function accountAction(text, action) {
  const control = document.createElement('button');
  control.type = 'button';
  control.className = 'demo-action';
  control.textContent = text;
  control.addEventListener('click', () => action(control));
  return control;
}

function updateAccountRole(row, role) {
  row.dataset.role = role;
  row.cells[2].textContent = accountRoles[role];
}

function accountLoginExists(login, exclude) {
  return [...accountBody.rows].some(row => row !== exclude &&
    row.querySelector('.user-login')?.textContent.trim().toLocaleLowerCase('vi') === login.toLocaleLowerCase('vi'));
}

accountCreate.removeAttribute('onsubmit');
accountCreate.addEventListener('submit', event => {
  event.preventDefault();
  const data = new FormData(accountCreate);
  const name = String(data.get('name')).trim();
  const login = String(data.get('login')).trim();
  const role = data.get('role_radio');
  const code = String(data.get('code')).trim() || 'TK-DEMO-' + (accountBody.rows.length + 1);
  if (!name || !login || !Object.hasOwn(accountRoles, role) || accountLoginExists(login) ||
      [...accountBody.rows].some(row => row.cells[0].textContent.trim() === code)) {
    window.alert('Kiểm tra tên, mã tài khoản, vai trò và tên đăng nhập. Mã và tên đăng nhập phải duy nhất.');
    return;
  }
  const row = accountBody.insertRow(0);
  row.insertCell().textContent = code;
  const identity = row.insertCell();
  const userName = document.createElement('strong');
  userName.className = 'user-name';
  userName.textContent = name;
  const userLogin = document.createElement('div');
  userLogin.className = 'user-login';
  userLogin.textContent = login;
  identity.append(userName, userLogin);
  row.insertCell();
  updateAccountRole(row, role);
  row.dataset.status = 'ACTIVE';
  row.insertCell().textContent = 'Hoạt động';
  row.insertCell().append(accountAction('Sửa', () =>
    openEditModal(code, userLogin.textContent, userName.textContent, '', row.dataset.role)));
  accountCreate.reset();
  closeModal('createModal');
  updateAccountCounts();
  filterTable();
  accountStatus.textContent = 'Đã thêm tài khoản mẫu ' + login + '.';
});

const editAccountForm = document.querySelector('#editModal form');
editAccountForm.removeAttribute('onsubmit');
editAccountForm.addEventListener('submit', event => {
  event.preventDefault();
  const id = document.querySelector('#editAccountId').value;
  const row = [...accountBody.rows].find(item => item.cells[0].textContent.trim() === id);
  const login = document.querySelector('#editLoginName').value.trim();
  const name = document.querySelector('#editFullName').value.trim();
  const role = document.querySelector('#editRoleSelect').value;
  if (!row || !login || !name || !Object.hasOwn(accountRoles, role) || accountLoginExists(login, row)) {
    window.alert('Kiểm tra họ tên, vai trò và tên đăng nhập duy nhất.');
    return;
  }
  row.querySelector('.user-name').textContent = name;
  row.querySelector('.user-login').textContent = login;
  updateAccountRole(row, role);
  const edit = row.querySelector('[onclick^="openEditModal"]');
  if (edit) {
    edit.removeAttribute('onclick');
    edit.addEventListener('click', () => openEditModal(id, row.querySelector('.user-login').textContent,
      row.querySelector('.user-name').textContent, '', row.dataset.role));
  }
  closeModal('editModal');
  updateAccountCounts();
  filterTable();
  accountStatus.textContent = 'Đã cập nhật tài khoản mẫu ' + login + '.';
});
