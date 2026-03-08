/**
 * المايسترو للرياضة - ملف المنطق البرمجي للواجهة الأمامية
 * يتضمن: إدارة السلة، المقاسات المتعددة، التحقق من الكوبونات، والربط مع SweetAlert2
 */

let currentProduct = null;
let appliedDiscount = 0;
let activeCouponCode = "";

/**
 * دالة التنبيهات السريعة (Toast) باستخدام SweetAlert2
 */
function toast(title, icon = 'success') {
    Swal.fire({
        title: title,
        icon: icon,
        timer: 3000,
        showConfirmButton: false,
        toast: true,
        position: 'top-end',
        timerProgressBar: true,
        background: '#151515',
        color: '#ffffff'
    });
}

/**
 * عرض الأقسام الرئيسية في الصفحة
 */
function renderCategories() {
    const container = document.getElementById('categories-render');
    if (!container) return;

    container.innerHTML = "";
    // ملاحظة: نستخدم الحروف الكبيرة (PascalCase) لتطابق بيانات C#
    categoriesData.forEach(cat => {
        container.innerHTML += `
            <div class="category-card" onclick="loadCategory(${cat.Id})">
                <div class="cat-icon ${cat.ColorClass}"><i class="fas ${cat.IconClass}"></i></div>
                <span style="color: #ffffff; font-weight: bold;">تفصيل ${cat.Name}</span>
            </div>
        `;
    });
}

/**
 * التنقل بين شاشات الموقع (الرئيسية، الأقسام، الموديلات)
 */
function toggleView(screenId) {
    document.querySelectorAll('section').forEach(s => s.classList.add('hidden'));
    const targetScreen = document.getElementById(screenId + '-screen');
    if (targetScreen) {
        targetScreen.classList.remove('hidden');
        targetScreen.classList.add('active-screen');
    }
    if (screenId === 'categories') renderCategories();
    window.scrollTo({ top: 0, behavior: 'smooth' });
}

/**
 * تحميل وعرض الموديلات داخل القسم المختار
 */
function loadCategory(catId) {
    const cat = categoriesData.find(c => c.Id === catId);
    if (!cat) return;

    const displayName = document.getElementById('cat-display-name');
    if (displayName) displayName.innerText = "تفصيل " + cat.Name;

    const list = document.getElementById('products-list');
    if (!list) return;

    list.innerHTML = "";

    if (!cat.Products || cat.Products.length === 0) {
        list.innerHTML = `<p style="text-align:center; width:100%; color:#D4AF37; font-size: 1.2rem; padding: 40px;">لا توجد موديلات في هذا القسم حالياً.</p>`;
    } else {
        cat.Products.forEach(item => {
            list.innerHTML += `
                <div class="product-item">
                    <div class="product-img">
                        <img src="/${item.ImageUrl}" alt="${item.Name}" onerror="this.src='https://placehold.co/400x500/111/D4AF37?text=No+Image'">
                    </div>
                    <div class="product-info">
                        <h3 style="color: #ffffff;">${item.Name} ${item.IsCustomDesign ? '<br><small style="color:#D4AF37; font-size:0.8rem;">(تصميم خاص)</small>' : ''}</h3>
                        <p style="color:#D4AF37; font-weight:bold; font-size:1.4rem; margin-bottom:15px;">${item.BasePrice} ريال</p>
                        <button class="btn-order" onclick="openOrderModal(${item.Id})">
                            <i class="fas fa-magic"></i> اطلب تفصيل الآن
                        </button>
                    </div>
                </div>
            `;
        });
    }
    toggleView('shop');
}

/**
 * فتح نافذة الطلب وتجهيز المقاسات والأسعار
 */
function openOrderModal(productId) {
    // البحث عن المنتج في البيانات المحملة
    categoriesData.forEach(c => {
        const found = c.Products.find(p => p.Id === productId);
        if (found) currentProduct = found;
    });

    if (!currentProduct) return;

    const modalTitle = document.getElementById('modal-item-name');
    if (modalTitle) modalTitle.innerText = "طلب تفصيل: " + currentProduct.Name;

    const alertBox = document.getElementById('custom-design-alert');
    if (alertBox) {
        if (currentProduct.IsCustomDesign) alertBox.classList.remove('hidden');
        else alertBox.classList.add('hidden');
    }

    const sizesTbody = document.getElementById('sizes-table-body');
    if (sizesTbody) {
        sizesTbody.innerHTML = `<tr><th style="color: #D4AF37;">المقاس</th><th style="color: #D4AF37;">السعر الإضافي</th><th style="color: #D4AF37;">الكمية</th></tr>`;
        sizesData.forEach(s => {
            let extraPriceVal = parseFloat(s.AdditionalPrice) || 0;
            let extraText = extraPriceVal > 0 ? `+${extraPriceVal} ريال` : (extraPriceVal < 0 ? `${extraPriceVal} ريال` : "مجاني");
            sizesTbody.innerHTML += `
                <tr>
                    <td style="color: #ffffff;">${s.Name} <br><small style="color:#ffffff; opacity: 0.7; font-size:0.7rem;">(${s.GroupName})</small></td>
                    <td style="color:#D4AF37; font-weight: bold;">${extraText}</td>
                    <td><input type="number" min="0" value="0" class="qty-input" data-size-id="${s.Id}" data-extra-price="${extraPriceVal}" onchange="calculateLiveTotal()" onkeyup="calculateLiveTotal()" style="color: #D4AF37; border-color: #D4AF37;"></td>
                </tr>
            `;
        });
    }

    // إعادة ضبط الحقول
    appliedDiscount = 0;
    activeCouponCode = "";
    document.getElementById('coupon-input').value = "";
    document.getElementById('order-notes').value = "";
    document.getElementById('fabric-select').value = "0";

    calculateLiveTotal();
    document.getElementById('order-modal').classList.remove('hidden');
}

/**
 * حساب الإجمالي بشكل فوري وتطبيق الخصومات
 */
function calculateLiveTotal(isCouponClick = false) {
    if (!currentProduct) return;

    let totalQty = 0;
    let totalPrice = 0;
    const fabricSelect = document.getElementById('fabric-select');
    const fabricExtra = fabricSelect ? parseFloat(fabricSelect.value) : 0;

    // حساب إجمالي الكميات والأسعار بناءً على كل مقاس
    document.querySelectorAll('.qty-input').forEach(input => {
        const qty = parseInt(input.value) || 0;
        const extraPrice = parseFloat(input.getAttribute('data-extra-price'));
        if (qty > 0) {
            totalQty += qty;
            totalPrice += qty * (currentProduct.BasePrice + extraPrice + fabricExtra);
        }
    });

    // رسوم التصميم الخاص: تضاف إذا كان المنتج IsCustomDesign والعدد <= 10 قطع
    if (currentProduct.IsCustomDesign && totalQty > 0 && totalQty <= 10) {
        totalPrice += (2 * totalQty);
    }

    // منطق التحقق من الكوبون من خلال السيرفر
    if (isCouponClick) {
        const codeInput = document.getElementById('coupon-input');
        const code = codeInput ? codeInput.value.trim().toUpperCase() : "";

        if (!code) {
            appliedDiscount = 0;
            activeCouponCode = "";
            updateDisplay(totalPrice);
            return;
        }

        fetch(`/Home/ValidateCoupon?code=${encodeURIComponent(code)}`)
            .then(res => res.json())
            .then(data => {
                if (data.valid) {
                    appliedDiscount = data.discount;
                    activeCouponCode = code;
                    Swal.fire({
                        title: 'تم تفعيل الخصم!',
                        text: `مبروك، حصلت على خصم ${data.discount} ريال`,
                        icon: 'success',
                        background: '#151515',
                        color: '#ffffff',
                        confirmButtonColor: '#D4AF37'
                    });
                } else {
                    appliedDiscount = 0;
                    activeCouponCode = "";
                    Swal.fire({
                        title: 'عفواً!',
                        text: 'كود الخصم غير صحيح أو منتهي الصلاحية',
                        icon: 'error',
                        background: '#151515',
                        color: '#ffffff',
                        confirmButtonColor: '#D4AF37'
                    });
                }
                updateDisplay(totalPrice);
            })
            .catch(err => {
                console.error("Coupon validation error:", err);
                toast("حدث خطأ في الاتصال بالسيرفر", "error");
            });
    } else {
        updateDisplay(totalPrice);
    }

    function updateDisplay(base) {
        let final = base - appliedDiscount;
        if (final < 0) final = 0;
        const display = document.getElementById('total-price-display');
        if (display) display.innerText = final.toFixed(2);
    }

    return { totalQty, totalPrice: (totalPrice - appliedDiscount) };
}

/**
 * إغلاق نافذة الطلب
 */
function closeModal() {
    const modal = document.getElementById('order-modal');
    if (modal) modal.classList.add('hidden');
}

/**
 * تأكيد الطلب الملكي: الحفظ في قاعدة البيانات ثم التوجه للواتساب
 */
function submitOrderProcess() {
    const { totalQty, totalPrice } = calculateLiveTotal();
    if (totalQty === 0) {
        Swal.fire({
            title: 'تنبيه',
            text: 'يرجى اختيار مقاس واحد وكمية على الأقل للمتابعة',
            icon: 'warning',
            background: '#151515',
            color: '#ffffff',
            confirmButtonColor: '#D4AF37'
        });
        return;
    }

    Swal.fire({
        title: 'تأكيد الطلب الملكي',
        text: `أنت على وشك طلب ${totalQty} قطعة، هل تود المتابعة؟`,
        icon: 'question',
        showCancelButton: true,
        confirmButtonColor: '#25D366',
        cancelButtonColor: '#d33',
        confirmButtonText: 'نعم، أرسل الطلب',
        cancelButtonText: 'تراجع',
        background: '#151515',
        color: '#ffffff'
    }).then((result) => {
        if (result.isConfirmed) {
            const btn = document.getElementById('submit-btn');
            if (btn) {
                btn.disabled = true;
                btn.innerHTML = '<i class="fas fa-spinner fa-spin"></i> جاري حفظ طلبك...';
            }

            const items = [];
            document.querySelectorAll('.qty-input').forEach(input => {
                const qty = parseInt(input.value) || 0;
                if (qty > 0) {
                    items.push({
                        SizeId: parseInt(input.getAttribute('data-size-id')),
                        Quantity: qty
                    });
                }
            });

            const fabricSelect = document.getElementById('fabric-select');
            const fabricName = fabricSelect ? fabricSelect.options[fabricSelect.selectedIndex].getAttribute('data-name') : "قياسي";
            const orderNotes = document.getElementById('order-notes');

            const payload = {
                ProductId: currentProduct.Id,
                Notes: orderNotes ? orderNotes.value : "",
                FabricType: fabricName,
                FabricExtraPrice: fabricSelect ? parseFloat(fabricSelect.value) : 0,
                CouponCode: activeCouponCode,
                Items: items
            };

            fetch('/Home/SubmitOrder', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
                        Swal.fire({
                            title: 'تم الحفظ بنجاح!',
                            text: 'جاري تحويلك الآن لتأكيد الطلب عبر الواتساب...',
                            icon: 'success',
                            showConfirmButton: false,
                            timer: 2500,
                            background: '#151515',
                            color: '#ffffff'
                        }).then(() => {
                            const msg = `*طلب تفصيل جديد من متجر المايسترو رقم ${data.orderId}*%0A---------------------------%0A*الموديل:* ${currentProduct.Name}%0A*نوع القماش:* ${fabricName}%0A*إجمالي القطع:* ${totalQty}%0A*موعد الاستلام المتوقع:* ${data.deliveryDate}%0A*الإجمالي النهائي:* ${data.totalPrice} ريال%0A---------------------------%0A*ملاحظات إضافية:* ${payload.Notes || 'لا توجد'}`;
                            window.location.href = `https://wa.me/96892111393?text=${msg}`;
                        });
                        closeModal();
                    } else {
                        Swal.fire('خطأ', data.message || 'حدثت مشكلة أثناء الحفظ', 'error');
                    }
                })
                .catch(err => {
                    console.error("Submit error:", err);
                    Swal.fire('خطأ', 'حدث فشل في الاتصال بالخادم، يرجى المحاولة لاحقاً', 'error');
                })
                .finally(() => {
                    if (btn) {
                        btn.disabled = false;
                        btn.innerHTML = '<i class="fab fa-whatsapp"></i> تأكيد الطلب والدفع عبر واتساب';
                    }
                });
        }
    });
}

/**
 * تحسينات الرؤية عند تحميل الصفحة (Style Guard)
 */
document.addEventListener('DOMContentLoaded', () => {
    const style = document.createElement('style');
    style.innerHTML = `
        /* ضمان أن النصوص لا تظهر بالرمادي أبداً */
        .modal-content *, .product-info *, .category-card *, .order-summary * {
            color: #ffffff !important;
        }
        .text-gold, #total-price-display, .cat-icon i, th {
            color: #D4AF37 !important;
        }
        .qty-input, textarea, select, input {
            color: #D4AF37 !important;
            background-color: #0a0a0a !important;
            border: 1px solid #D4AF37 !important;
        }
        small, .text-muted, p {
            color: #ffffff !important;
            opacity: 0.85;
        }
        ::placeholder {
            color: #ffffff !important;
            opacity: 0.4;
        }
    `;
    document.head.appendChild(style);
});