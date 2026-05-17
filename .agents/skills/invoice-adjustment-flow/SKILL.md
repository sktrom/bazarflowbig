# Skill: Invoice Adjustment Flow Agent

## Overview
This skill focuses purely on the strict backend and frontend logic limitations around handling completed invoices. 
الهدف من هذه المهارة هو منع التعديل المباشر وحفظ أثر التعديل بشكل رسمي وواضح، وليس مجرد سجل فروقات ثانوي.

## Capabilities expected
- **Mutation Rejection:** الفاتورة المكتملة لا تُفتح مباشرة أبدًا.
- **Adjustment Constraints:** 
  - يتم إنشاء Request Adjustment منفصل.
  - بعد الرفض لا يُسمح بطلب تعديل جديد لنفس الفاتورة.
- **الأنواع المعتمدة (Adjustment Types):**
  - `DeleteLine`
  - `ChangeQuantity`
  - `ChangeLineTotal`
  - `CancelInvoice`
- **Approval Actions (عند الموافقة):**
  - `DeleteLine` => يرجع مخزون السطر
  - `ChangeQuantity` => تُسوّى فقط فروقات الكمية
  - `ChangeLineTotal` => تعديل مالي فقط بدون تغيير مخزون
  - `CancelInvoice` => يرجع كامل المخزون
- **Post-Approval State Changes:** 
  - بعد القبول تصبح الفاتورة `Modified` أو `Cancelled` حسب النوع.
- **Transactions:** 
  - كل approve/reject والأثر الناتج يجب أن يكون داخل transaction واحدة.
