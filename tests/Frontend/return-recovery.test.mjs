import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';
import { runInNewContext } from 'node:vm';

const source = readFileSync(new URL('../../src/Pos.Web/wwwroot/js/pos-returns.js', import.meta.url), 'utf8');
const key = 'pos-return:browser-session:cashier-1';
const body = JSON.stringify({
    maHoaDon: 'HD-001',
    maYeuCau: 'same-request',
    maCa: 'CA-001',
    phienBan: 0,
    dongTra: [{ dong: 0, soLuong: 1, nhapLai: 1 }]
});
const record = JSON.stringify({ confirm: true, body });

function loadRecovery({ stored = new Map(), actor = 'cashier-1', withForm = false, storageBlocked = false,
    fetch = () => assert.fail('Unexpected request') } = {}) {
    const elements = new Map();
    function get(id) {
        if (!elements.has(id)) {
            elements.set(id, {
                hidden: true,
                disabled: false,
                dataset: {},
                handlers: {},
                contains: () => false,
                focus() {},
                addEventListener(name, handler) { this.handlers[name] = handler; }
            });
        }
        return elements.get(id);
    }

    const panel = get('return-write');
    panel.dataset = { actor, session: 'browser-session', save: '/TraHang/Ghi', lookup: '/TraHang/TraCuu' };
    panel.querySelector = () => ({ value: 'new-token' });
    const form = withForm ? get('return-form') : null;
    if (form) {
        form.dataset = { invoice: 'HD-001', shift: 'CA-001', version: '0' };
        form.querySelector = () => get('fieldset');
        const qty = { value: '1', valueAsNumber: 1, max: '3' };
        const stock = { value: '1' };
        const controls = { '.qty': qty, '.stock': stock, '.subtotal': get('subtotal') };
        form.querySelectorAll = () => [{ dataset: { line: '0', price: '12000' }, querySelector: selector => controls[selector] }];
        get('mode').value = 'cash';
    }
    runInNewContext(source, {
        AbortSignal,
        document: { getElementById: get, querySelector: () => form },
        sessionStorage: {
            setItem(name, value) {
                if (storageBlocked) throw new Error('Storage unavailable');
                stored.set(name, value);
            },
            getItem: name => stored.get(name),
            removeItem: name => stored.delete(name)
        },
        window: { addEventListener() {}, crypto: { randomUUID: () => 'initial-request' } },
        fetch
    });
    return get;
}

test('recovery can replay outside the missing form with the exact previous request', async () => {
    const stored = new Map([[key, record]]);
    const get = loadRecovery({
        stored,
        fetch: async (url, request) => {
            assert.equal(url, '/TraHang/Ghi?xacNhan=true');
            assert.equal(request.headers.RequestVerificationToken, 'new-token');
            assert.equal(request.body, body);
            return {
                ok: true,
                status: 200,
                json: async () => ({ message: 'Saved', ma: 'PT-001', traCuuUrl: '/TraHang/TraCuu?chon=PT-001' })
            };
        }
    });

    assert.equal(get('return-recovery').hidden, false);
    await get('retry').handlers.click();
    assert.equal(stored.has(key), false);
    assert.equal(get('return-recovery').hidden, true);
    assert.equal(get('return-receipt').hidden, false);
    assert.equal(get('receipt').href, '/TraHang/TraCuu?chon=PT-001');
});

test('an uncertain or unauthorized result keeps the request available for replay', async () => {
    const stored = new Map([[key, record]]);
    const responses = [
        { status: 503 },
        { status: 403, json: async () => ({ message: 'Access denied' }) }
    ];
    const get = loadRecovery({ stored, fetch: async () => responses.shift() });

    for (let i = 0; i < 2; i++) {
        await get('retry').handlers.click();
        assert.equal(stored.get(key), record);
        assert.equal(get('return-recovery').hidden, false);
        assert.equal(get('retry').disabled, false);
    }
});

test('corrupt storage does not prevent looking up other invoices', () => {
    const stored = new Map([[key, '{"confirm":true,"body":"incomplete-json"}']]);
    const get = loadRecovery({ stored });
    assert.equal(stored.has(key), false);
    assert.equal(get('return-recovery').hidden, true);
    assert.equal(get('feedback').hidden, false);
});

test('recovery never displays another account snapshot', () => {
    const stored = new Map([[key, record]]);
    const get = loadRecovery({ stored, actor: 'cashier-2' });
    assert.equal(stored.get(key), record);
    assert.equal(get('return-recovery').hidden, true);
    assert.equal(get('feedback').hidden, true);
});

test('an uncertain first submit persists the exact request before fetch and reload', async () => {
    const stored = new Map();
    let submitted;
    const get = loadRecovery({
        stored,
        withForm: true,
        fetch: async (_, request) => {
            submitted = request.body;
            assert.equal(JSON.parse(stored.get(key)).body, submitted);
            return { status: 503 };
        }
    });
    get('return-form').handlers.submit({ preventDefault() {}, submitter: { value: 'confirm' } });
    assert.equal(JSON.parse(submitted).maYeuCau, 'initial-request');
    assert.equal(JSON.parse(submitted).tienMat, 12000);

    let replayed;
    const reloaded = loadRecovery({
        stored,
        fetch: async (_, request) => { replayed = request.body; return { status: 503 }; }
    });
    await reloaded('retry').handlers.click();
    assert.equal(replayed, submitted);
});

test('blocked storage opens invoice search in another tab while retaining the uncertain request', async () => {
    const requests = [];
    const get = loadRecovery({
        withForm: true,
        storageBlocked: true,
        fetch: async (_, request) => { requests.push(request.body); return { status: 503 }; }
    });
    get('return-form').handlers.submit({ preventDefault() {}, submitter: { value: 'confirm' } });
    await new Promise(resolve => setImmediate(resolve));
    assert.equal(get('return-search-form').target, '_blank');
    assert.equal(get('return-memory-only').hidden, false);
    await get('retry').handlers.click();
    assert.equal(requests.length, 2);
    assert.equal(requests[0], requests[1]);
});
