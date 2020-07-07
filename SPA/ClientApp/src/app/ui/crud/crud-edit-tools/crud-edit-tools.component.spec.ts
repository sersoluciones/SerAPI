import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CrudEditToolsComponent } from './crud-edit-tools.component';

describe('CrudEditToolsComponent', () => {
  let component: CrudEditToolsComponent;
  let fixture: ComponentFixture<CrudEditToolsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CrudEditToolsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CrudEditToolsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
