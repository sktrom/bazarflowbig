# Known Limitations

* **Reports Export:** تصدير التقارير غير منفذ حاليًا إذا لم يكن موجودًا في الواجهة.
* **Print / PDF:** طباعة الفواتير أو تصديرها كـ PDF غير منفذ حاليًا.
* **Offers Product Selection:** في الإصدار V1، يعتمد اختيار المنتج في العروض على إدخال الـ Product ID يدويًا، مع عرض رقم المنتج في شاشتي المنتجات (Products) والمخزون (Inventory) لتسهيل الوصول إليه.
* **Pagination:** لا توجد pagination متقدمة في بعض الجداول.
* **Device Code:** المتغير `deviceCode` في V1 ثابت كـ `DEFAULT_DEVICE`.
* **Store Settings:** إعدادات المتجر مثل `StoreName` و `ExchangeRate` في شاشة Settings معروضة للقراءة فقط (read-only) حاليًا من الواجهة.
* **Secrets Management:** ملفات الإعدادات المتتبعة لا يجب أن تحتوي كلمات مرور قاعدة البيانات. يجب ضبط أسرار الإنتاج عبر environment variables أو secret manager قبل أي release حقيقي.
* **Reverse Proxy / Certificates:** إعدادات `ForwardedHeaders` وإدارة شهادات HTTPS الخاصة بالنشر ليست مضمنة بعد، وتبقى مسؤولية مرحلة deployment لاحقة.
* **Production Readiness:** هذه النسخة مناسبة للتشغيل المحلي/التجريبي وليست production نهائية، ويُنصح بعدم إطلاقها للجمهور بدون مراجعة أمنية شاملة لبيئة الإنتاج.
