/**
 * Shared JavaScript components for HLS communication nodes
 * Common UI components and utilities
 */

(function(window) {
  'use strict';

  // Global HLS UI namespace
  window.HLS = window.HLS || {};
  window.HLS.UI = window.HLS.UI || {};

  /**
   * Validation utilities
   */
  window.HLS.UI.Validators = {
    
    // IP address validation
    validateIP: function(ip) {
      if (!ip) return { valid: false, message: 'IP地址不能为空' };
      
      const ipRegex = /^(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)$/;
      if (!ipRegex.test(ip)) {
        return { valid: false, message: '请输入有效的IP地址格式 (例如: 192.168.1.100)' };
      }
      
      return { valid: true, message: 'IP地址格式正确' };
    },

    // Port number validation
    validatePort: function(port) {
      if (!port) return { valid: false, message: '端口号不能为空' };
      
      const portNum = parseInt(port);
      if (isNaN(portNum) || portNum < 1 || portNum > 65535) {
        return { valid: false, message: '端口号必须在1-65535之间' };
      }
      
      return { valid: true, message: '端口号有效' };
    },

    // Modbus address validation
    validateModbusAddress: function(address) {
      if (!address) return { valid: false, message: '地址不能为空' };
      
      // Support various Modbus address formats
      const addressRegex = /^[0-9]{1,6}$/;
      if (!addressRegex.test(address)) {
        return { valid: false, message: '请输入有效的Modbus地址 (例如: 40001)' };
      }
      
      const addr = parseInt(address);
      if (addr < 1 || addr > 999999) {
        return { valid: false, message: 'Modbus地址范围应在1-999999之间' };
      }
      
      return { valid: true, message: '地址格式正确' };
    },

    // Data type validation
    validateDataType: function(dataType) {
      const validTypes = ['Bool', 'Int16', 'Int32', 'Int64', 'UInt16', 'UInt32', 'UInt64', 'Float', 'Double', 'String'];
      if (!validTypes.includes(dataType)) {
        return { valid: false, message: '不支持的数据类型' };
      }
      return { valid: true, message: '数据类型有效' };
    },

    // Timeout validation
    validateTimeout: function(timeout) {
      if (!timeout) return { valid: false, message: '超时时间不能为空' };
      
      const timeoutNum = parseInt(timeout);
      if (isNaN(timeoutNum) || timeoutNum < 1000 || timeoutNum > 60000) {
        return { valid: false, message: '超时时间应在1000-60000毫秒之间' };
      }
      
      return { valid: true, message: '超时时间有效' };
    }
  };

  /**
   * Form validation helper
   */
  window.HLS.UI.FormValidator = function(formElement) {
    this.form = formElement;
    this.validators = {};
    this.isValid = true;
  };

  window.HLS.UI.FormValidator.prototype = {
    addField: function(fieldName, element, validator, required = true) {
      this.validators[fieldName] = {
        element: element,
        validator: validator,
        required: required
      };
      
      // Add real-time validation
      const self = this;
      $(element).on('blur change', function() {
        self.validateField(fieldName);
      });
      
      return this;
    },

    validateField: function(fieldName) {
      const field = this.validators[fieldName];
      if (!field) return true;

      const value = $(field.element).val();
      const $formGroup = $(field.element).closest('.hls-form-control');
      
      // Clear previous validation state
      $formGroup.removeClass('has-error has-success has-warning');
      $formGroup.find('.hls-error-message, .hls-success-message, .hls-warning-message').remove();

      // Required field check
      if (field.required && !value) {
        this.showFieldError($formGroup, '此字段为必填项');
        return false;
      }

      if (value && field.validator) {
        const result = field.validator(value);
        if (!result.valid) {
          this.showFieldError($formGroup, result.message);
          return false;
        } else {
          this.showFieldSuccess($formGroup, result.message);
          return true;
        }
      }

      return true;
    },

    validateAll: function() {
      let allValid = true;
      for (let fieldName in this.validators) {
        if (!this.validateField(fieldName)) {
          allValid = false;
        }
      }
      this.isValid = allValid;
      return allValid;
    },

    showFieldError: function($formGroup, message) {
      $formGroup.addClass('has-error');
      $formGroup.append('<span class="hls-error-message">' + message + '</span>');
    },

    showFieldSuccess: function($formGroup, message) {
      $formGroup.addClass('has-success');
      if (message) {
        $formGroup.append('<span class="hls-success-message">' + message + '</span>');
      }
    },

    showFieldWarning: function($formGroup, message) {
      $formGroup.addClass('has-warning');
      $formGroup.append('<span class="hls-warning-message">' + message + '</span>');
    }
  };

  /**
   * Data points table component
   */
  window.HLS.UI.DataPointsTable = function(container, options) {
    this.container = $(container);
    this.options = $.extend({
      showDefaultValue: false,
      showReadWrite: false,
      dataTypes: ['Bool', 'Int16', 'Int32', 'Int64', 'UInt16', 'UInt32', 'UInt64', 'Float', 'Double', 'String']
    }, options || {});
    this.dataPoints = [];
    this.init();
  };

  window.HLS.UI.DataPointsTable.prototype = {
    init: function() {
      this.render();
      this.bindEvents();
    },

    render: function() {
      const headers = ['地址', '数据类型', '名称', '描述'];
      if (this.options.showDefaultValue) headers.splice(3, 0, '默认值');
      if (this.options.showReadWrite) headers.splice(-1, 0, '读写');
      headers.push('操作');

      let headerHtml = '<div class="hls-data-points-header">';
      headers.forEach(header => {
        headerHtml += '<div>' + header + '</div>';
      });
      headerHtml += '</div>';

      const html = `
        <div class="hls-data-points-container">
          ${headerHtml}
          <div class="hls-data-points-body"></div>
        </div>
        <div style="margin-top: 10px;">
          <button type="button" class="hls-btn hls-btn-primary hls-add-point">
            <i class="fa fa-plus"></i> 添加数据点
          </button>
          <button type="button" class="hls-btn hls-btn-secondary hls-import-points" style="margin-left: 10px;">
            <i class="fa fa-upload"></i> 批量导入
          </button>
        </div>
      `;

      this.container.html(html);
      this.bodyContainer = this.container.find('.hls-data-points-body');
    },

    bindEvents: function() {
      const self = this;
      
      // Add point button
      this.container.find('.hls-add-point').on('click', function() {
        self.addDataPoint();
      });

      // Import points button
      this.container.find('.hls-import-points').on('click', function() {
        self.showImportDialog();
      });
    },

    addDataPoint: function(data = {}) {
      const point = {
        address: data.address || '',
        dataType: data.dataType || 'Int16',
        name: data.name || '',
        description: data.description || '',
        defaultValue: data.defaultValue || '',
        readWrite: data.readWrite || 'read'
      };

      this.dataPoints.push(point);
      this.renderDataPoint(point, this.dataPoints.length - 1);
    },

    renderDataPoint: function(point, index) {
      const dataTypeOptions = this.options.dataTypes.map(type => 
        `<option value="${type}" ${type === point.dataType ? 'selected' : ''}>${this.getDataTypeLabel(type)}</option>`
      ).join('');

      let cellsHtml = `
        <div>
          <input type="text" class="hls-data-point-input address-input" value="${point.address}" placeholder="40001" />
        </div>
        <div>
          <select class="hls-data-point-select datatype-select">${dataTypeOptions}</select>
        </div>
        <div>
          <input type="text" class="hls-data-point-input name-input" value="${point.name}" placeholder="温度传感器" />
        </div>
      `;

      if (this.options.showDefaultValue) {
        cellsHtml += `
          <div>
            <input type="text" class="hls-data-point-input defaultvalue-input" value="${point.defaultValue}" placeholder="0" />
          </div>
        `;
      }

      if (this.options.showReadWrite) {
        cellsHtml += `
          <div>
            <select class="hls-data-point-select readwrite-select">
              <option value="read" ${point.readWrite === 'read' ? 'selected' : ''}>只读</option>
              <option value="write" ${point.readWrite === 'write' ? 'selected' : ''}>只写</option>
              <option value="readwrite" ${point.readWrite === 'readwrite' ? 'selected' : ''}>读写</option>
            </select>
          </div>
        `;
      }

      cellsHtml += `
        <div>
          <input type="text" class="hls-data-point-input description-input" value="${point.description}" placeholder="数据点描述" />
        </div>
        <div class="hls-data-point-actions">
          <button type="button" class="hls-remove-point" data-index="${index}">删除</button>
        </div>
      `;

      const rowHtml = `<div class="hls-data-point-row" data-index="${index}">${cellsHtml}</div>`;
      this.bodyContainer.append(rowHtml);

      this.bindRowEvents(index);
    },

    bindRowEvents: function(index) {
      const self = this;
      const $row = this.bodyContainer.find(`[data-index="${index}"]`);

      // Remove button
      $row.find('.hls-remove-point').on('click', function() {
        self.removeDataPoint(index);
      });

      // Input change events
      $row.find('input, select').on('change blur', function() {
        self.updateDataPoint(index);
      });

      // Address validation
      $row.find('.address-input').on('blur', function() {
        const address = $(this).val();
        const result = window.HLS.UI.Validators.validateModbusAddress(address);
        const $input = $(this);
        
        $input.removeClass('has-error has-success');
        $input.next('.hls-error-message').remove();
        
        if (!result.valid && address) {
          $input.addClass('has-error');
          $input.after('<div class="hls-error-message">' + result.message + '</div>');
        } else if (result.valid) {
          $input.addClass('has-success');
        }
      });
    },

    updateDataPoint: function(index) {
      const $row = this.bodyContainer.find(`[data-index="${index}"]`);
      const point = this.dataPoints[index];

      point.address = $row.find('.address-input').val();
      point.dataType = $row.find('.datatype-select').val();
      point.name = $row.find('.name-input').val();
      point.description = $row.find('.description-input').val();

      if (this.options.showDefaultValue) {
        point.defaultValue = $row.find('.defaultvalue-input').val();
      }

      if (this.options.showReadWrite) {
        point.readWrite = $row.find('.readwrite-select').val();
      }
    },

    removeDataPoint: function(index) {
      this.dataPoints.splice(index, 1);
      this.refresh();
    },

    refresh: function() {
      this.bodyContainer.empty();
      this.dataPoints.forEach((point, index) => {
        this.renderDataPoint(point, index);
      });
    },

    getDataPoints: function() {
      // Update all data points before returning
      this.dataPoints.forEach((_, index) => {
        this.updateDataPoint(index);
      });
      return this.dataPoints;
    },

    setDataPoints: function(dataPoints) {
      this.dataPoints = dataPoints || [];
      this.refresh();
    },

    getDataTypeLabel: function(type) {
      const labels = {
        'Bool': '布尔',
        'Int16': '16位整数',
        'Int32': '32位整数', 
        'Int64': '64位整数',
        'UInt16': '16位无符号整数',
        'UInt32': '32位无符号整数',
        'UInt64': '64位无符号整数',
        'Float': '单精度浮点数',
        'Double': '双精度浮点数',
        'String': '字符串'
      };
      return labels[type] || type;
    },

    showImportDialog: function() {
      const self = this;
      const dialogHtml = `
        <div class="hls-import-dialog" style="position: fixed; top: 0; left: 0; width: 100%; height: 100%; 
             background: rgba(0,0,0,0.5); z-index: 2000; display: flex; align-items: center; justify-content: center;">
          <div style="background: white; padding: 20px; border-radius: 8px; max-width: 600px; width: 90%;">
            <h3>批量导入数据点</h3>
            <p>请粘贴JSON格式的数据点配置：</p>
            <textarea id="import-data" rows="10" style="width: 100%; font-family: monospace; font-size: 12px;"></textarea>
            <div style="margin-top: 15px; text-align: right;">
              <button type="button" class="hls-btn hls-btn-secondary cancel-import">取消</button>
              <button type="button" class="hls-btn hls-btn-primary confirm-import" style="margin-left: 10px;">导入</button>
            </div>
          </div>
        </div>
      `;

      $('body').append(dialogHtml);

      $('.cancel-import').on('click', function() {
        $('.hls-import-dialog').remove();
      });

      $('.confirm-import').on('click', function() {
        try {
          const data = JSON.parse($('#import-data').val());
          if (Array.isArray(data)) {
            self.setDataPoints(data);
            $('.hls-import-dialog').remove();
          } else {
            alert('导入数据必须是数组格式');
          }
        } catch (e) {
          alert('JSON格式错误：' + e.message);
        }
      });
    }
  };

  /**
   * Connection test utility
   */
  window.HLS.UI.ConnectionTester = function(options) {
    this.options = options || {};
  };

  window.HLS.UI.ConnectionTester.prototype = {
    test: function(config, callback) {
      const $status = $('.hls-connection-status');
      if ($status.length === 0) {
        const statusHtml = '<div class="hls-connection-status testing">正在测试连接...</div>';
        $('.hls-form-section').first().append(statusHtml);
      } else {
        $status.removeClass('success error').addClass('testing').text('正在测试连接...');
      }

      // Simulate connection test (in real implementation, this would make an API call)
      setTimeout(() => {
        const success = Math.random() > 0.3; // 70% success rate for demo
        const $status = $('.hls-connection-status');
        
        if (success) {
          $status.removeClass('testing error').addClass('success').text('连接测试成功！');
          callback && callback(null, { success: true, message: '连接正常' });
        } else {
          $status.removeClass('testing success').addClass('error').text('连接失败：无法连接到目标设备');
          callback && callback(new Error('连接失败'), null);
        }
      }, 2000);
    }
  };

  /**
   * Configuration templates
   */
  window.HLS.UI.Templates = {
    modbusDefaults: {
      device: {
        protocol: 'ModbusTcp',
        host: '192.168.1.100',
        port: 502,
        timeout: 5000
      },
      dataPoints: [
        { address: '40001', dataType: 'Int16', name: 'AI1', description: '模拟输入1' },
        { address: '40002', dataType: 'Int16', name: 'AI2', description: '模拟输入2' }
      ]
    },

    opcuaDefaults: {
      device: {
        protocol: 'OpcUa',
        host: '192.168.1.100',
        port: 4840,
        timeout: 10000
      },
      dataPoints: [
        { address: 'ns=2;s=Channel1.Device1.Tag1', dataType: 'Int16', name: 'Tag1', description: 'OPC UA标签1' },
        { address: 'ns=2;s=Channel1.Device1.Tag2', dataType: 'Float', name: 'Tag2', description: 'OPC UA标签2' }
      ]
    },

    getTemplate: function(templateName) {
      switch (templateName) {
        case 'modbus-tcp':
          return this.modbusDefaults;
        case 'opc-ua':
          return this.opcuaDefaults;
        default:
          return null;
      }
    }
  };

  /**
   * Import/Export utilities
   */
  window.HLS.UI.ImportExport = {
    exportConfiguration: function(config, filename) {
      const dataStr = JSON.stringify(config, null, 2);
      const blob = new Blob([dataStr], { type: 'application/json' });
      const url = URL.createObjectURL(blob);
      
      const link = document.createElement('a');
      link.href = url;
      link.download = filename || 'hls-config.json';
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      URL.revokeObjectURL(url);
    },

    importConfiguration: function(callback) {
      const input = document.createElement('input');
      input.type = 'file';
      input.accept = '.json';
      
      input.onchange = function(e) {
        const file = e.target.files[0];
        if (!file) return;

        const reader = new FileReader();
        reader.onload = function(e) {
          try {
            const config = JSON.parse(e.target.result);
            callback(null, config);
          } catch (err) {
            callback(new Error('配置文件格式错误：' + err.message));
          }
        };
        reader.readAsText(file);
      };

      input.click();
    }
  };

})(window);