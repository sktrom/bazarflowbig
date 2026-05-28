import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SettingsComponent } from './settings.component';
import { SettingsApiService } from '../services/settings-api.service';
import { of, throwError } from 'rxjs';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { SessionService } from '../../../core/services/session.service';
import { BlackBoxRecorderService } from '../../../core/services/black-box-recorder.service';

describe('SettingsComponent', () => {
  let component: SettingsComponent;
  let fixture: ComponentFixture<SettingsComponent>;
  let apiSpy: jasmine.SpyObj<SettingsApiService>;
  let authSpy: jasmine.SpyObj<AuthService>;
  let sessionSpy: jasmine.SpyObj<SessionService>;
  let routerSpy: jasmine.SpyObj<Router>;
  let blackBoxSpy: jasmine.SpyObj<BlackBoxRecorderService>;

  const mockEmployees = [
    { id: 1, fullName: 'Ali', username: 'ali', isActive: true, createdAt: '2024-01-01', phone: null }
  ];
  const mockDetail = {
    id: 1, fullName: 'Ali', username: 'ali', isActive: true, createdAt: '', updatedAt: '', phone: null,
    permissions: [
      { screenId: 1, screenKey: 'Sales', screenName: 'Sales', canAccess: true },
      { screenId: 2, screenKey: 'Products', screenName: 'Products', canAccess: false }
    ]
  };

  beforeEach(async () => {
    const spy = jasmine.createSpyObj('SettingsApiService', [
      'getEmployees','getEmployee','createEmployee','updateEmployee','deleteEmployee','resetPassword',
      'getCategories','createCategory','updateCategory','deleteCategory','getPublicSettings','createBackup',
      'getAuditLogs','getAuditLogsStatus','getAuditLog',
      'getDevices','getDevice','createDevice','updateDevice','enableDevice','disableDevice','deleteDevice',
      'getActiveSessions', 'forceCloseSession'
    ]);
    spy.getEmployees.and.returnValue(of({ items: mockEmployees }));
    spy.getCategories.and.returnValue(of({ items: [] }));
    spy.getPublicSettings.and.returnValue(of({ storeName: 'Test Store', exchangeRate: 15000 }));
    spy.getAuditLogs.and.returnValue(of({ items: [], totalCount: 0, page: 1, pageSize: 50 }));
    spy.getAuditLogsStatus.and.returnValue(of({ totalCount: 150, oldestCreatedAt: '2026-05-01T10:00:00Z', newestCreatedAt: '2026-05-21T11:00:00Z', approximateLargeJsonCount: 20, recommendedRetentionDays: 180, cleanupEnabled: false }));
    spy.getAuditLog.and.returnValue(of({ id: 1, action: 'CREATE', entityType: 'Product', createdAt: '2026-05-20T14:30:12', hasBefore: false, hasAfter: false, hasMetadata: false }));
    spy.getDevices.and.returnValue(of([]));
    spy.getDevice.and.returnValue(of({ id: 1, deviceCode: 'POS-01', deviceName: 'Register 1', isActive: true, notes: '', createdAt: '', updatedAt: '' }));
    spy.createDevice.and.returnValue(of({ id: 1, deviceCode: 'POS-01', deviceName: 'Register 1', isActive: true, notes: '', createdAt: '', updatedAt: '' }));
    spy.updateDevice.and.returnValue(of({ id: 1, deviceCode: 'POS-01', deviceName: 'Register 1', isActive: true, notes: '', createdAt: '', updatedAt: '' }));
    spy.enableDevice.and.returnValue(of({ success: true, message: 'Device enabled' }));
    spy.disableDevice.and.returnValue(of({ success: true, message: 'Device disabled' }));
    spy.deleteDevice.and.returnValue(of({ success: true, message: 'DEVICE_DELETED' }));
    spy.getActiveSessions.and.returnValue(of([]));
    spy.forceCloseSession.and.returnValue(of({}));

    const authServiceSpy = jasmine.createSpyObj('AuthService', ['setAuthenticated', 'isLoggedIn', 'logout']);
    const sessionServiceSpy = jasmine.createSpyObj('SessionService', ['getSessionId', 'clearSession']);
    const routerServiceSpy = jasmine.createSpyObj('Router', ['navigate']);
    const blackBoxServiceSpy = jasmine.createSpyObj('BlackBoxRecorderService', ['recordSuccess', 'recordFailure']);

    sessionServiceSpy.getSessionId.and.returnValue('100');

    await TestBed.configureTestingModule({
      imports: [SettingsComponent, FormsModule],
      providers: [
        { provide: SettingsApiService, useValue: spy },
        { provide: AuthService, useValue: authServiceSpy },
        { provide: SessionService, useValue: sessionServiceSpy },
        { provide: Router, useValue: routerServiceSpy },
        { provide: BlackBoxRecorderService, useValue: blackBoxServiceSpy }
      ]
    }).compileComponents();

    apiSpy = TestBed.inject(SettingsApiService) as jasmine.SpyObj<SettingsApiService>;
    authSpy = TestBed.inject(AuthService) as jasmine.SpyObj<AuthService>;
    sessionSpy = TestBed.inject(SessionService) as jasmine.SpyObj<SessionService>;
    routerSpy = TestBed.inject(Router) as jasmine.SpyObj<Router>;
    blackBoxSpy = TestBed.inject(BlackBoxRecorderService) as jasmine.SpyObj<BlackBoxRecorderService>;

    fixture = TestBed.createComponent(SettingsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should load employees on init (default tab)', () => {
    expect(apiSpy.getEmployees).toHaveBeenCalled();
    expect(component.employees.length).toBe(1);
    expect(component.activeTab).toBe('employees');
  });

  it('should show empty state for categories', () => {
    component.switchTab('categories');
    expect(apiSpy.getCategories).toHaveBeenCalled();
    expect(component.categories.length).toBe(0);
  });

  it('should show store settings on store tab', () => {
    component.switchTab('store');
    expect(apiSpy.getPublicSettings).toHaveBeenCalled();
    expect(component.storeSettings?.storeName).toBe('Test Store');
    expect(component.storeSettings?.exchangeRate).toBe(15000);
  });

  it('should display developer attribution on store tab', () => {
    component.switchTab('store');
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('أياز مراد');
    expect(compiled.textContent).toContain('Ayaz Murad');
  });

  it('should render backup tab', () => {
    component.switchTab('backup');
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent;
    expect(text).toContain('النسخ الاحتياطي');
    expect(text).toContain('إنشاء نسخة احتياطية');
  });

  it('should call create backup and render success metadata', () => {
    apiSpy.createBackup.and.returnValue(of({
      success: true,
      fileName: 'BazarFlow_Backup_20260520_143012.bak',
      createdAt: '2026-05-20T14:30:12',
      sizeBytes: 1048576,
      message: 'Backup created successfully.',
      backupDirectory: 'C:\\BazarFlowBackups'
    }));

    component.switchTab('backup');
    component.createBackup();
    fixture.detectChanges();

    expect(apiSpy.createBackup).toHaveBeenCalled();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('BazarFlow_Backup_20260520_143012.bak');
    expect(text).toContain('C:\\BazarFlowBackups');
    expect(text).toContain('1.00 MB');
  });

  it('should render backup error message', () => {
    const err = new HttpErrorResponse({ error: { error: 'BACKUP_SQL_FAILED' }, status: 500 });
    apiSpy.createBackup.and.returnValue(throwError(() => err));

    component.switchTab('backup');
    component.createBackup();
    fixture.detectChanges();

    expect(component.backupError).toBe('فشل تنفيذ النسخ الاحتياطي في SQL Server');
    expect(fixture.nativeElement.textContent).toContain('فشل تنفيذ النسخ الاحتياطي في SQL Server');
  });

  it('should open create employee modal', () => {
    component.openEmpCreate();
    expect(component.activeModal).toBe('empCreate');
    expect(component.editEmpId).toBeNull();
  });

  it('should include Purchases in allScreens and render it as a checkbox in employee modal', () => {
    expect(component.allScreens).toContain('Purchases');
    
    component.openEmpCreate();
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement;
    const labels = compiled.querySelectorAll('label.flex.items-center.gap-2.cursor-pointer');
    const labelTexts = Array.from(labels).map((el: any) => el.textContent.trim());
    
    expect(labelTexts).toContain('Purchases');
  });

  it('should load permissions when opening edit modal', () => {
    apiSpy.getEmployee.and.returnValue(of(mockDetail as any));
    component.openEmpEdit(mockEmployees[0] as any);
    expect(apiSpy.getEmployee).toHaveBeenCalledWith(1);
    expect(component.empPermissions['Sales']).toBeTrue();
    expect(component.empPermissions['Products']).toBeFalse();
    expect(component.activeModal).toBe('empEdit');
  });

  it('should toggle permission checkbox', () => {
    component.empPermissions = { Sales: true };
    component.togglePermission('Sales');
    expect(component.empPermissions['Sales']).toBeFalse();
  });

  it('should map USERNAME_ALREADY_EXISTS error in create', () => {
    const err = new HttpErrorResponse({ error: { error: 'USERNAME_ALREADY_EXISTS' }, status: 409 });
    apiSpy.createEmployee.and.returnValue(throwError(() => err));
    component.empForm = { fullName: 'Ali', username: 'ali', password: '123' };
    component.activeModal = 'empCreate';
    component.saveEmployee();
    expect(component.formErr).toBe('اسم المستخدم موجود مسبقًا');
  });

  it('should show CANNOT_DELETE_SELF as toast on delete', () => {
    const err = new HttpErrorResponse({ error: { error: 'CANNOT_DELETE_SELF' }, status: 409 });
    apiSpy.deleteEmployee.and.returnValue(throwError(() => err));
    component.actionEmp = mockEmployees[0] as any;
    component.activeModal = 'empDelete';
    component.confirmDeleteEmployee();
    expect(component.toast).toBe('لا يمكنك حذف حسابك الحالي');
  });

  it('should open category create modal', () => {
    component.openCatCreate();
    expect(component.activeModal).toBe('catCreate');
    expect(component.catForm.name).toBe('');
  });

  it('should map CATEGORY_NAME_ALREADY_EXISTS error', () => {
    const err = new HttpErrorResponse({ error: { error: 'CATEGORY_NAME_ALREADY_EXISTS' }, status: 409 });
    apiSpy.createCategory.and.returnValue(throwError(() => err));
    component.catForm = { name: 'Electronics' };
    component.activeModal = 'catCreate';
    component.saveCategory();
    expect(component.formErr).toBe('اسم التصنيف موجود مسبقًا');
  });

  it('should render audit logs tab and switch successfully', () => {
    component.switchTab('audit');
    expect(component.activeTab).toBe('audit');
    expect(apiSpy.getAuditLogs).toHaveBeenCalled();
    expect(apiSpy.getAuditLogsStatus).toHaveBeenCalled();
  });

  it('should display audit logs status card and warning message in UI', () => {
    const datePipe = new DatePipe('en-US');
    const expectedOldest = datePipe.transform('2026-05-01T10:00:00Z', 'yyyy-MM-dd HH:mm');
    const expectedNewest = datePipe.transform('2026-05-21T11:00:00Z', 'yyyy-MM-dd HH:mm');

    component.switchTab('audit');
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('حالة وتخزين سجلات النشاط');
    expect(text).toContain('150');
    expect(text).toContain(expectedOldest!);
    expect(text).toContain(expectedNewest!);
    expect(text).toContain('التنظيف التلقائي غير مفعّل في هذه النسخة. لا تحذف السجلات إلا بعد عمل Backup كامل للنظام.');
  });

  it('should load audit logs with filter params when filtered', () => {
    component.switchTab('audit');
    component.filterAction = 'CREATE';
    component.filterEntityType = 'Product';
    component.filterEmployeeId = 5;
    component.filterDateFrom = '2026-05-01';
    component.filterDateTo = '2026-05-31';

    component.loadAuditLogs();
    
    expect(apiSpy.getAuditLogs).toHaveBeenCalledWith({
      page: 1,
      pageSize: 50,
      action: 'CREATE',
      entityType: 'Product',
      employeeId: 5,
      dateFrom: new Date('2026-05-01').toISOString(),
      dateTo: new Date('2026-05-31').toISOString()
    });
  });

  it('should paginate to next page', () => {
    component.auditPage = 1;
    component.auditPageSize = 50;
    component.auditTotalCount = 120;
    
    component.changeAuditPage(1);
    expect(component.auditPage).toBe(2);
    expect(apiSpy.getAuditLogs).toHaveBeenCalled();
  });

  it('should open audit detail modal and show detail response', () => {
    const mockDetailResponse = {
      id: 77,
      action: 'CREATE_BACKUP',
      entityType: 'Backup',
      createdAt: '2026-05-20T14:30:12',
      hasBefore: false,
      hasAfter: false,
      hasMetadata: true,
      metadataJson: '{"file":"test.bak"}'
    };
    apiSpy.getAuditLog.and.returnValue(of(mockDetailResponse));

    component.openAuditDetail(77);
    expect(apiSpy.getAuditLog).toHaveBeenCalledWith(77);
    expect(component.selectedAuditLog?.action).toBe('CREATE_BACKUP');
    expect(component.activeModal).toBe('auditDetail');
  });

  it('should display empty state when audit logs count is zero', () => {
    component.switchTab('audit');
    component.auditLogs = [];
    fixture.detectChanges();
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('لا توجد سجلات مطابقة للفلاتر المحددة.');
  });

  it('should translate actions to Arabic', () => {
    expect(component.translateAction('CREATE_BACKUP')).toBe('إنشاء نسخة احتياطية');
    expect(component.translateAction('RESET_PASSWORD')).toBe('إعادة تعيين كلمة المرور');
    expect(component.translateAction('UNKNOWN_ACTION')).toBe('UNKNOWN_ACTION');
  });

  it('should switch to devices tab and load devices list', () => {
    const mockDevices = [
      { id: 10, deviceCode: 'POS-10', deviceName: 'Counter 10', isActive: true, createdAt: '2026-05-19T23:00:00' }
    ];
    apiSpy.getDevices.and.returnValue(of(mockDevices));

    component.switchTab('devices');
    expect(component.activeTab).toBe('devices');
    expect(apiSpy.getDevices).toHaveBeenCalled();
    expect(component.devices.length).toBe(1);
    expect(component.devices[0].deviceName).toBe('Counter 10');
  });

  it('should open create device modal', () => {
    component.openDeviceCreate();
    expect(component.activeModal).toBe('deviceCreate');
    expect(component.editDeviceId).toBeNull();
    expect(component.deviceForm.deviceCode).toBe('');
  });

  it('should save new device successfully', () => {
    component.activeModal = 'deviceCreate';
    component.deviceForm = { deviceCode: 'POS-NEW', deviceName: 'New POS Register', notes: 'Main area' };
    
    component.saveDevice();
    expect(apiSpy.createDevice).toHaveBeenCalledWith({
      deviceCode: 'POS-NEW',
      deviceName: 'New POS Register',
      notes: 'Main area'
    });
  });

  it('should toggle device activation state', () => {
    const d = { id: 10, deviceCode: 'POS-10', deviceName: 'Counter 10', isActive: true, createdAt: '2026-05-19T23:00:00' };
    component.toggleDeviceActive(d);
    expect(apiSpy.disableDevice).toHaveBeenCalledWith(10);
  });

  it('should delete device or show deactivated toast if used', () => {
    component.actionDevice = { id: 12, deviceCode: 'POS-12', deviceName: 'Counter 12', isActive: true, createdAt: '2026-05-19' };
    component.activeModal = 'deviceDelete';
    
    // Test physical delete success
    apiSpy.deleteDevice.and.returnValue(of({ success: true, message: 'DEVICE_DELETED' }));
    component.confirmDeleteDevice();
    expect(apiSpy.deleteDevice).toHaveBeenCalledWith(12);
    expect(component.toast).toBe('تم حذف الجهاز بنجاح');
  });

  it('should show default device badge in HTML template when deviceCode is DEFAULT_DEVICE', () => {
    component.switchTab('devices');
    component.devices = [
      { id: 1, deviceCode: 'DEFAULT_DEVICE', deviceName: 'Default Register', isActive: true, createdAt: '2026-05-19' }
    ];
    fixture.detectChanges();
    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('جهاز النظام الافتراضي');
  });

  it('should show toast error message when deleting DEFAULT_DEVICE', () => {
    const d = { id: 1, deviceCode: 'DEFAULT_DEVICE', deviceName: 'Default Register', isActive: true, createdAt: '2026-05-19' };
    component.openDeviceDelete(d);
    expect(component.toast).toBe('لا يمكن حذف جهاز النظام الافتراضي');
  });

  it('should map CANNOT_DELETE_DEFAULT_DEVICE error and display in toast', () => {
    const err = new HttpErrorResponse({ error: { error: 'CANNOT_DELETE_DEFAULT_DEVICE' }, status: 400 });
    apiSpy.deleteDevice.and.returnValue(throwError(() => err));
    component.actionDevice = { id: 1, deviceCode: 'DEFAULT_DEVICE', deviceName: 'Default Register', isActive: true, createdAt: '2026-05-19' };
    component.activeModal = 'deviceDelete';
    component.confirmDeleteDevice();
    expect(component.toast).toBe('لا يمكن حذف جهاز النظام الافتراضي');
  });

  it('should map CANNOT_DISABLE_LAST_ACTIVE_DEVICE error and display in toast', () => {
    const err = new HttpErrorResponse({ error: { error: 'CANNOT_DISABLE_LAST_ACTIVE_DEVICE' }, status: 400 });
    apiSpy.disableDevice.and.returnValue(throwError(() => err));
    const d = { id: 10, deviceCode: 'POS-10', deviceName: 'Counter 10', isActive: true, createdAt: '2026-05-19' };
    component.toggleDeviceActive(d);
    expect(component.toast).toBe('لا يمكن تعطيل آخر جهاز نشط');
  });

  const mockSessions = [
    {
      sessionId: 100,
      employeeId: 1,
      employeeName: 'Ali',
      username: 'ali',
      deviceId: 10,
      deviceCode: 'POS-10',
      deviceName: 'Counter 10',
      startedAt: '2026-05-20T10:00:00Z',
      lastSeenAt: '2026-05-21T11:00:00Z',
      expiresAt: '2026-05-22T10:00:00Z'
    },
    {
      sessionId: 200,
      employeeId: 2,
      employeeName: 'Omar',
      username: 'omar',
      deviceId: 20,
      deviceCode: 'POS-20',
      deviceName: 'Counter 20',
      startedAt: '2026-05-20T11:00:00Z',
      lastSeenAt: '2026-05-21T12:00:00Z',
      expiresAt: '2026-05-22T11:00:00Z'
    }
  ];

  it('should render active sessions tab', () => {
    const text = fixture.nativeElement.textContent;
    expect(text).toContain('الجلسات النشطة');
  });

  it('should call getActiveSessions when switching to sessions tab', () => {
    apiSpy.getActiveSessions.and.returnValue(of([]));
    component.switchTab('sessions');
    expect(apiSpy.getActiveSessions).toHaveBeenCalled();
  });

  it('should display active sessions list in the table', () => {
    apiSpy.getActiveSessions.and.returnValue(of(mockSessions));
    component.switchTab('sessions');
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('Omar');
    expect(compiled.textContent).toContain('ali');
    expect(compiled.textContent).toContain('Counter 20');
  });

  it('should display current session badge for current session', () => {
    sessionSpy.getSessionId.and.returnValue('100');
    apiSpy.getActiveSessions.and.returnValue(of(mockSessions));
    component.switchTab('sessions');
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('جلستك الحالية');
  });

  it('should show correct confirmation message for regular session force close', () => {
    sessionSpy.getSessionId.and.returnValue('100');
    component.openSessionCloseConfirm(mockSessions[1]); // Omar, sessionId 200
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('هل أنت متأكد من رغبتك في إغلاق هذه الجلسة فوراً؟ سيتم تسجيل خروج الموظف وقد يفقد أي بيانات غير محفوظة.');
    expect(compiled.textContent).not.toContain('تنبيه: أنت تقوم بإغلاق جلستك الحالية');
  });

  it('should show correct warning message for current session force close', () => {
    sessionSpy.getSessionId.and.returnValue('100');
    component.openSessionCloseConfirm(mockSessions[0]); // Ali, sessionId 100
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    expect(compiled.textContent).toContain('تنبيه: أنت تقوم بإغلاق جلستك الحالية. سيؤدي هذا إلى تسجيل خروجك فورًا من النظام. هل تريد الاستمرار؟');
  });

  it('should call forceCloseSession and close modal for regular session', () => {
    component.actionSession = mockSessions[1]; // Omar, sessionId 200
    component.activeModal = 'sessionConfirmClose';
    apiSpy.forceCloseSession.and.returnValue(of({}));
    apiSpy.getActiveSessions.and.returnValue(of([]));

    component.confirmForceCloseSession();

    expect(apiSpy.forceCloseSession).toHaveBeenCalledWith(200);
    expect(component.activeModal).toBeNull();
    expect(apiSpy.getActiveSessions).toHaveBeenCalled();
    expect(component.toast).toBe('تم إغلاق الجلسة بنجاح');
  });

  it('should clear session and redirect to login on current session force close', () => {
    sessionSpy.getSessionId.and.returnValue('100');
    component.actionSession = mockSessions[0]; // Ali, sessionId 100
    component.activeModal = 'sessionConfirmClose';
    apiSpy.forceCloseSession.and.returnValue(of({}));

    component.confirmForceCloseSession();

    expect(apiSpy.forceCloseSession).toHaveBeenCalledWith(100);
    expect(sessionSpy.clearSession).toHaveBeenCalled();
    expect(authSpy.setAuthenticated).toHaveBeenCalledWith(false);
    expect(routerSpy.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('should map SESSION_NOT_FOUND, SESSION_NOT_ACTIVE, and NO_ACTIVE_SESSION errors', () => {
    component.actionSession = mockSessions[1]; // Omar, sessionId 200
    component.activeModal = 'sessionConfirmClose';

    // 1. SESSION_NOT_FOUND
    let err = new HttpErrorResponse({ error: { error: 'SESSION_NOT_FOUND' }, status: 404 });
    apiSpy.forceCloseSession.and.returnValue(throwError(() => err));
    component.confirmForceCloseSession();
    expect(component.formErr).toBe('الجلسة غير موجودة');

    // 2. SESSION_NOT_ACTIVE
    err = new HttpErrorResponse({ error: { error: 'SESSION_NOT_ACTIVE' }, status: 400 });
    apiSpy.forceCloseSession.and.returnValue(throwError(() => err));
    component.confirmForceCloseSession();
    expect(component.formErr).toBe('الجلسة ليست نشطة');

    // 3. NO_ACTIVE_SESSION
    err = new HttpErrorResponse({ error: { error: 'NO_ACTIVE_SESSION' }, status: 400 });
    apiSpy.forceCloseSession.and.returnValue(throwError(() => err));
    component.confirmForceCloseSession();
    expect(component.formErr).toBe('لا توجد جلسة نشطة');

    // 4. Fallback
    err = new HttpErrorResponse({ error: { error: 'UNKNOWN_ERROR' }, status: 500 });
    apiSpy.forceCloseSession.and.returnValue(throwError(() => err));
    component.confirmForceCloseSession();
    expect(component.formErr).toBe('حدث خطأ أثناء إغلاق الجلسة');
  });
});
