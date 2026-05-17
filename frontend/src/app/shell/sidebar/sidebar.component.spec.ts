import { ComponentFixture, TestBed } from '@angular/core/testing';
import { SidebarComponent } from './sidebar.component';
import { PermissionsService } from '../../core/services/permissions.service';
import { BehaviorSubject } from 'rxjs';
import { RouterTestingModule } from '@angular/router/testing';
import { By } from '@angular/platform-browser';

describe('SidebarComponent', () => {
  let component: SidebarComponent;
  let fixture: ComponentFixture<SidebarComponent>;
  let mockPermissionsSubject: BehaviorSubject<string[]>;

  beforeEach(async () => {
    mockPermissionsSubject = new BehaviorSubject<string[]>(['Dashboard', 'Products']);
    const permSpy = jasmine.createSpyObj('PermissionsService', [], {
      permissions$: mockPermissionsSubject.asObservable()
    });

    await TestBed.configureTestingModule({
      imports: [SidebarComponent, RouterTestingModule],
      providers: [
        { provide: PermissionsService, useValue: permSpy }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(SidebarComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should render only links user has permission for', () => {
    const links = fixture.debugElement.queryAll(By.css('a'));
    // Based on menuItems in sidebar: Dashboard, Cashier, Invoices, Inventory, Products, Offers, Reports
    // User has 'Dashboard' and 'Products'
    expect(links.length).toBe(2);
    expect(links[0].nativeElement.textContent.trim()).toBe('الرئيسية');
    expect(links[1].nativeElement.textContent.trim()).toBe('المنتجات');
  });

  it('should update dynamically when permissions change', () => {
    mockPermissionsSubject.next(['Dashboard', 'Reports']);
    fixture.detectChanges();

    const links = fixture.debugElement.queryAll(By.css('a'));
    expect(links.length).toBe(2);
    expect(links[0].nativeElement.textContent.trim()).toBe('الرئيسية');
    expect(links[1].nativeElement.textContent.trim()).toBe('التقارير');
  });
});
