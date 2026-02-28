// DOM Ready
document.addEventListener('DOMContentLoaded', function() {
  // Sidebar Toggle
  const sidebar = document.querySelector('.main-sidebar');
  const content = document.querySelector('.content');
  const footer = document.querySelector('.main-footer');
  const sidebarToggle = document.getElementById('sidebarToggle');

  // Check for saved sidebar state
  const isCollapsed = localStorage.getItem('sidebarCollapsed') === 'true';

  if (isCollapsed) {
    sidebar.classList.add('collapsed');
    content.classList.add('collapsed');
    footer.classList.add('collapsed');
  }

  // Toggle Sidebar
  sidebarToggle.addEventListener('click', function() {
    sidebar.classList.toggle('collapsed');
    content.classList.toggle('collapsed');
    footer.classList.toggle('collapsed');

    // Save state
    localStorage.setItem('sidebarCollapsed', sidebar.classList.contains('collapsed'));
  });

  // Mobile sidebar toggle
  function handleMobileSidebar() {
    if (window.innerWidth < 768) {
      sidebar.classList.add('collapsed');
      content.classList.add('collapsed');
      footer.classList.add('collapsed');
    }
  }

  // Initial check
  handleMobileSidebar();

  // Window resize listener
  window.addEventListener('resize', handleMobileSidebar);

  // User Profile Dropdown
  const userProfile = document.querySelector('.user-profile');
  const profileInfo = document.querySelector('.profile-info');

  profileInfo.addEventListener('click', function(e) {
    e.preventDefault();
    e.stopPropagation();
    userProfile.classList.toggle('active');
  });

  // Close dropdown when clicking outside
  document.addEventListener('click', function(e) {
    if (!userProfile.contains(e.target)) {
      userProfile.classList.remove('active');
    }
  });

  // Sidebar Dropdowns
  const dropdowns = document.querySelectorAll('.dropdown');

  dropdowns.forEach(function(dropdown) {
    const link = dropdown.querySelector('a:first-child');

    link.addEventListener('click', function(e) {
      e.preventDefault();

      // Close other dropdowns
      dropdowns.forEach(function(otherDropdown) {
        if (otherDropdown !== dropdown) {
          otherDropdown.classList.remove('active');
        }
      });

      // Toggle current dropdown
      dropdown.classList.toggle('active');
    });
  });

  // Close all dropdowns when clicking outside
  document.addEventListener('click', function(e) {
    if (!e.target.closest('.dropdown')) {
      dropdowns.forEach(function(dropdown) {
        dropdown.classList.remove('active');
      });
    }
  });
  document.querySelector('.header-right').prepend(darkModeToggle);

  const darkModeBtn = document.getElementById('darkModeToggle');
  const darkModeIcon = darkModeBtn.querySelector('i');

  // Check for saved dark mode preference
  if (localStorage.getItem('darkMode') === 'true') {
    document.body.classList.add('dark-mode');
    darkModeIcon.classList.remove('fa-moon');
    darkModeIcon.classList.add('fa-sun');
  }

  // Toggle Dark Mode
  darkModeBtn.addEventListener('click', function() {
    document.body.classList.toggle('dark-mode');

    // Update icon
    if (document.body.classList.contains('dark-mode')) {
      darkModeIcon.classList.remove('fa-moon');
      darkModeIcon.classList.add('fa-sun');
    } else {
      darkModeIcon.classList.remove('fa-sun');
      darkModeIcon.classList.add('fa-moon');
    }

    // Save preference
    localStorage.setItem('darkMode', document.body.classList.contains('dark-mode'));
  });

  // Search Bar Focus Effect
  const searchBar = document.querySelector('.search-bar input');

  searchBar.addEventListener('focus', function() {
    this.parentElement.classList.add('focused');
  });

  searchBar.addEventListener('blur', function() {
    this.parentElement.classList.remove('focused');
  });

  // Initialize Charts
  if (document.getElementById('attendanceChart')) {
    new Chart(document.getElementById('attendanceChart'), {
      type: 'line',
      data: {
        labels: ['Week 1', 'Week 2', 'Week 3', 'Week 4'],
        datasets: [{
          label: 'Attendance',
          data: [85, 88, 90, 92],
          borderColor: '#1E3A8A',
          backgroundColor: 'rgba(30, 58, 138, 0.1)',
          borderWidth: 2,
          fill: true,
          tension: 0.4
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: {
            position: 'top',
          },
          tooltip: {
            mode: 'index',
            intersect: false,
          }
        },
        scales: {
          y: {
            beginAtZero: false,
            min: 80,
            max: 100
          }
        }
      }
    });
  }

  if (document.getElementById('feesChart')) {
    new Chart(document.getElementById('feesChart'), {
      type: 'bar',
      data: {
        labels: ['Jan', 'Feb', 'Mar', 'Apr'],
        datasets: [{
          label: 'Fees Collected',
          data: [5000, 7000, 6000, 8000],
          backgroundColor: '#10B981',
          borderRadius: 4
        }]
      },
      options: {
        responsive: true,
        plugins: {
          legend: {
            position: 'top',
          }
        },
        scales: {
          y: {
            beginAtZero: true
          }
        }
      }
    });
  }

  // Notification Badge Animation
  const notificationBadge = document.querySelector('.notifications .badge');
  if (notificationBadge) {
    notificationBadge.style.transform = 'scale(0)';
    setTimeout(() => {
      notificationBadge.style.transition = 'transform 0.3s ease';
      notificationBadge.style.transform = 'scale(1)';
    }, 500);
  }

  // Auto-hide alerts
  const alerts = document.querySelectorAll('.alert');
  alerts.forEach(alert => {
    setTimeout(() => {
      alert.style.transition = 'opacity 0.5s ease';
      alert.style.opacity = '0';
      setTimeout(() => alert.remove(), 500);
    }, 5000);
  });
});