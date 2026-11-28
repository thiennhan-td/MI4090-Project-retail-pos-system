const screens = JSON.parse(document.querySelector('#screen-data').textContent);
const frame = document.querySelector('#screen-frame');
const dropdown = document.querySelector('#screen-picker');
const links = [...document.querySelectorAll('[data-screen]')];
let currentView;

function renderScreen() {
  const [id, filters = ''] = location.hash.slice(1).split('?');
  const screen = screens.find(item => item.id === id) ?? screens[0];
  const url = new URL(screen.html, location.href);
  if (screen.id === id) url.search = filters;
  if (currentView === url.href) return;
  currentView = url.href;
  const title = screen.code + ' · ' + screen.title;
  document.querySelector('#current-title').textContent = title;
  document.querySelector('#screen-open').href = currentView;
  const original = document.querySelector('#screen-original');
  original.hidden = !screen.original;
  if (screen.original) original.href = screen.img;
  else original.removeAttribute('href');
  frame.src = currentView;
  frame.title = title;
  dropdown.value = screen.id;
  for (const link of links) {
    if (link.dataset.screen === screen.id) link.setAttribute('aria-current', 'page');
    else link.removeAttribute('aria-current');
  }
}

for (const link of links) {
  link.addEventListener('click', event => {
    if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
    event.preventDefault();
    location.hash = link.dataset.screen;
  });
}
dropdown.addEventListener('change', () => { location.hash = dropdown.value; });
window.addEventListener('hashchange', renderScreen);
renderScreen();
