import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { CommonOptionComponent } from './common-option.component';

describe('CommonCrudComponent', () => {
  let component: CommonOptionComponent;
  let fixture: ComponentFixture<CommonOptionComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ CommonOptionComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(CommonOptionComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
