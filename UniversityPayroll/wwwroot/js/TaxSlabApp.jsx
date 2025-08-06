function initialForm() {
  var slabs = [];
  for (var i = 0; i < 6; i++) {
    slabs.push({ from: 0, to: null, rate: 0 });
  }
  return { financialYear: "", cessPercent: 0, slabs: slabs };
}

function TaxSlabApp() {
  var [taxSlabs, setTaxSlabs] = React.useState(window.taxSlabsData);
  var [showForm, setShowForm] = React.useState(false);
  var [editingId, setEditingId] = React.useState(null);
  var [formData, setFormData] = React.useState(initialForm());

  function resetForm() {
    setFormData(initialForm());
    setEditingId(null);
  }

  function handleSubmit(ev) {
    ev.preventDefault();
    var url = editingId ? "/TaxSlab/EditAjax" : "/TaxSlab/CreateAjax";
    var data = formData;
    data.slabs = formData.slabs.filter(function (s) { return s.rate > 0; });
    data.id = editingId;
    fetch(url, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(data)
    }).then(function () {
      window.location.reload();
    });
  }

  function updateField(field, value) {
    var newData = {};
    for (var key in formData) {
      newData[key] = formData[key];
    }
    newData[field] = value;
    setFormData(newData);
  }

  function updateSlab(i, field, val) {
    var newData = {};
    for (var key in formData) {
      newData[key] = formData[key];
    }
    var newSlabs = [];
    for (var j = 0; j < formData.slabs.length; j++) {
      newSlabs[j] = {};
      for (var k in formData.slabs[j]) {
        newSlabs[j][k] = formData.slabs[j][k];
      }
    }
    newSlabs[i][field] = val === "" ? null : Number(val) || 0;
    newData.slabs = newSlabs;
    setFormData(newData);
  }

  function showNewForm() {
    resetForm();
    setShowForm(true);
  }

  function showEditForm(id) {
    fetch("/TaxSlab/GetById?id=" + id).then(function (res) {
      return res.json();
    }).then(function (data) {
      while (data.slabs.length < 6) {
        data.slabs.push({ from: 0, to: null, rate: 0 });
      }
      setFormData(data);
      setEditingId(id);
      setShowForm(true);
    });
  }

  function deleteTaxSlab(id) {
    if (!confirm("Delete?")) return;
    fetch("/TaxSlab/DeleteAjax", { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(id) });
    setTaxSlabs(taxSlabs.filter(function (t) { return t.id !== id; }));
  }

  function renderForm() {
    return (
      <div className="container mt-3">
        <h2>{editingId ? "Edit Tax Slab" : "New Tax Slab"}</h2>
        <form onSubmit={handleSubmit}>
          <div>Financial Year</div>
          <input type="text" className="form-control mb-2" value={formData.financialYear} onChange={function(e) { updateField("financialYear", e.target.value); }} required />
          <div>Cess %</div>
          <input type="number" className="form-control mb-3" value={formData.cessPercent || ""} onChange={function(e) { updateField("cessPercent", Number(e.target.value)); }} required />
          <table className="table table-bordered">
            <thead>
              <tr><th>From</th><th>To</th><th>Rate</th></tr>
            </thead>
            <tbody>
              {formData.slabs.map(function(slab, i) {
                return (
                  <tr key={i}>
                    <td><input type="number" className="form-control" value={slab.from || ""} onChange={function(e) { updateSlab(i, "from", e.target.value); }} /></td>
                    <td><input type="number" className="form-control" value={slab.to || ""} onChange={function(e) { updateSlab(i, "to", e.target.value); }} /></td>
                    <td><input type="number" className="form-control" value={slab.rate || ""} onChange={function(e) { updateSlab(i, "rate", e.target.value); }} /></td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <button type="submit" className="btn btn-primary me-2">{editingId ? "Update" : "Save"}</button>
          <button type="button" className="btn btn-secondary" onClick={function() { setShowForm(false); }}>Cancel</button>
        </form>
      </div>
    );
  }

  function renderTable() {
    return (
      <div className="container mt-3">
        <h2>Tax Slabs</h2>
        <button className="btn btn-primary mb-3" onClick={showNewForm}>New Tax Slab</button>
        <table className="table table-striped">
          <thead>
            <tr><th>FY</th><th>Cess</th><th>Slabs</th><th>Actions</th></tr>
          </thead>
          <tbody>
            {taxSlabs.map(function(t) {
              return (
                <tr key={t.id}>
                  <td>{t.financialYear}</td>
                  <td>{t.cessPercent}</td>
                  <td>{t.slabs.map(function(s) { return s.from + "-" + (s.to || "∞") + " @@ " + s.rate + "%"; }).join("; ")}</td>
                  <td>
                    <button className="btn btn-sm btn-secondary me-1" onClick={function() { showEditForm(t.id); }}>Edit</button>
                    <button className="btn btn-sm btn-danger" onClick={function() { deleteTaxSlab(t.id); }}>Delete</button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    );
  }

  return showForm ? renderForm() : renderTable();
}

ReactDOM.createRoot(document.getElementById("react-root")).render(<TaxSlabApp />);
