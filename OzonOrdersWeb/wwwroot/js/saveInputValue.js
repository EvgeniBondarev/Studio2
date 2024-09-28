document.addEventListener('DOMContentLoaded', (event) => {
    // ������� ��� ���������� �������� ���� � ���� �� 1 ����
    function saveToCookies(key, value) {
        const expires = new Date();
        expires.setTime(expires.getTime() + (24 * 60 * 60 * 1000)); // 1 ����
        document.cookie = `${key}=${encodeURIComponent(value)}; expires=${expires.toUTCString()}; path=/`;
    }

    // ������� ��� �������� �������� �� ����
    function loadFromCookies(key) {
        const name = key + "=";
        const decodedCookie = decodeURIComponent(document.cookie);
        const ca = decodedCookie.split(';');
        for (let i = 0; i < ca.length; i++) {
            let c = ca[i].trim();
            if (c.indexOf(name) === 0) {
                return c.substring(name.length, c.length);
            }
        }
        return "";
    }

    // ������������ ���� 'statusSelect' �� ��������
    const statusSelectElement = document.getElementById('statusSelect');
    if (statusSelectElement) {
        const savedStatusValue = loadFromCookies(statusSelectElement.id);

        // ������������� �������� �� �����, ���� ��� ����
        if (savedStatusValue) {
            statusSelectElement.value = savedStatusValue;
        }

        // ��������� � ���� ��� ���������
        statusSelectElement.addEventListener('change', function () {
            saveToCookies(this.id, this.value);
        });
    }

    // ��� ������� select � ������� 'status-select'
    document.querySelectorAll('.status-select').forEach(select => {
        const id = select.id;
        const savedValue = loadFromCookies(id);

        // ���� ���� ����������� ��������, ������������� ���
        if (savedValue) {
            select.value = savedValue;
        }

        // ��������� �������� � ���� ��� ���������
        select.addEventListener('change', function () {
            saveToCookies(id, this.value);
        });
    });

    // ��� ������� select � ������� 'supplier-select'
    document.querySelectorAll('.supplier-select').forEach(select => {
        const id = select.id;
        const savedValue = loadFromCookies(id);

        if (savedValue) {
            select.value = savedValue;
        }

        select.addEventListener('change', function () {
            saveToCookies(id, this.value);
        });
    });

    // ��� ������� input � ������� 'orderNumberToSupplie-field'
    document.querySelectorAll('.orderNumberToSupplie-field').forEach(input => {
        const id = input.id;
        const savedValue = loadFromCookies(id);

        if (savedValue) {
            input.value = savedValue;
        }

        input.addEventListener('input', function () {
            saveToCookies(id, this.value);
        });
    });

    // ��� ������� input � ������� 'product-weight-field'
    document.querySelectorAll('.product-weight-field').forEach(input => {
        const id = input.id;
        const savedValue = loadFromCookies(id);

        if (savedValue) {
            input.value = savedValue;
        }

        input.addEventListener('input', function () {
            saveToCookies(id, this.value);
        });
    });

    // ��� ������� input � ������� 'purchase-price-field'
    document.querySelectorAll('.purchase-price-field').forEach(input => {
        const id = input.id;
        const savedValue = loadFromCookies(id);

        if (savedValue) {
            input.value = savedValue;
        }

        input.addEventListener('input', function () {
            saveToCookies(id, this.value);
        });
    });

    // ��� ������� input � ������� 'ozon-commission-field'
    document.querySelectorAll('.ozon-commission-field').forEach(input => {
        const id = input.id;
        const savedValue = loadFromCookies(id);

        if (savedValue) {
            input.value = savedValue;
        }

        input.addEventListener('input', function () {
            saveToCookies(id, this.value);
        });
    });
});
