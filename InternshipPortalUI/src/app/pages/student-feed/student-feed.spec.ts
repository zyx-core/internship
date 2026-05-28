import { ComponentFixture, TestBed } from '@angular/core/testing';

import { StudentFeed } from './student-feed';

describe('StudentFeed', () => {
  let component: StudentFeed;
  let fixture: ComponentFixture<StudentFeed>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StudentFeed],
    }).compileComponents();

    fixture = TestBed.createComponent(StudentFeed);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
